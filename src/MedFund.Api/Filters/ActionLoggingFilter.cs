using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MedFund.Api.Filters;

public sealed class ActionLoggingFilter : IAsyncActionFilter
{
    private static readonly string[] SensitiveTerms =
    [
        "password",
        "token",
        "secret",
        "authorization",
        "apikey",
        "apiKey"
    ];

    private readonly ILogger<ActionLoggingFilter> logger;

    public ActionLoggingFilter(ILogger<ActionLoggingFilter> logger)
    {
        this.logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var actionName = context.ActionDescriptor is ControllerActionDescriptor descriptor
            ? $"{descriptor.ControllerName}Controller.{descriptor.ActionName}"
            : context.ActionDescriptor.DisplayName ?? "UnknownAction";

        var sanitizedArguments = context.ActionArguments.ToDictionary(
            pair => pair.Key,
            pair => SanitizeValue(pair.Key, pair.Value));

        logger.LogInformation(
            "Controller action received request. Action={Action}, Route={Route}, Method={Method}, Data={@Data}",
            actionName,
            context.HttpContext.Request.Path.Value,
            context.HttpContext.Request.Method,
            sanitizedArguments);

        var executedContext = await next();

        if (executedContext.Exception is null)
        {
            logger.LogInformation(
                "Controller action completed. Action={Action}, StatusCode={StatusCode}",
                actionName,
                context.HttpContext.Response.StatusCode);
        }
        else
        {
            logger.LogWarning(
                executedContext.Exception,
                "Controller action failed. Action={Action}",
                actionName);
        }
    }

    private static object? SanitizeValue(string name, object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (IsSensitive(name))
        {
            return "***";
        }

        if (value is IFormFile file)
        {
            return new
            {
                file.FileName,
                file.ContentType,
                file.Length
            };
        }

        var type = value.GetType();
        if (type.IsPrimitive || value is string or Guid or DateOnly or DateTime or DateTimeOffset or decimal)
        {
            return value;
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            return new
            {
                Count = enumerable.Cast<object>().Take(101).Count()
            };
        }

        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetIndexParameters().Length == 0)
            .ToDictionary(
                property => property.Name,
                property => SanitizeValue(property.Name, property.GetValue(value)));
    }

    private static bool IsSensitive(string name)
    {
        return SensitiveTerms.Any(term => name.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
