using MedFund.Application.Common;
using MedFund.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MedFund.Infrastructure.Services;

public abstract class ServiceBase
{
    protected ServiceBase(IApplicationDbContext db, ICurrentUserAccessor currentUserAccessor, IAuditWriter auditWriter, ILogger logger)
    {
        Db = db;
        CurrentUserAccessor = currentUserAccessor;
        AuditWriter = auditWriter;
        Logger = logger;
    }

    protected IApplicationDbContext Db { get; }

    protected ICurrentUserAccessor CurrentUserAccessor { get; }

    protected IAuditWriter AuditWriter { get; }

    protected ILogger Logger { get; }

    protected CurrentUser CurrentUser => CurrentUserAccessor.GetRequiredCurrentUser();

    protected void LogReceived(string functionName, object? data = null)
    {
        Logger.LogInformation(
            "{Service}.{Function} received request. Data={@Data}",
            GetType().Name,
            functionName,
            data);
    }

    protected void LogKey(string functionName, string message, object? data = null)
    {
        Logger.LogInformation(
            "{Service}.{Function} {Message}. Data={@Data}",
            GetType().Name,
            functionName,
            message,
            data);
    }

    protected CurrentUser RequireRole(UserRole role)
    {
        var current = CurrentUser;
        if (RoleNames.ParseRole(current.Role) != role)
        {
            Logger.LogWarning(
                "{Service}.{Function} rejected role access. RequiredRole={RequiredRole}, ActualRole={ActualRole}, UserId={UserId}",
                GetType().Name,
                nameof(RequireRole),
                role,
                current.Role,
                current.UserId);
            throw new ForbiddenException("The current user is not allowed to perform this action.");
        }

        return current;
    }

    protected static Guid ParseId(string value, string fieldName)
    {
        if (!Guid.TryParse(value, out var id))
        {
            throw new MedFundException($"{fieldName} must be a valid UUID.");
        }

        return id;
    }
}
