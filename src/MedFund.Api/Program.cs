using System.Text;
using FluentValidation;
using MedFund.Api.Filters;
using MedFund.Api.Json;
using MedFund.Api.Middleware;
using MedFund.Api.Security;
using MedFund.Api.Startup;
using MedFund.Application.Validation;
using MedFund.Infrastructure;
using MedFund.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

UseRenderDatabaseUrl(builder.Configuration);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<MedFund.Application.Common.ICurrentUserAccessor>(provider => provider.GetRequiredService<CurrentUserAccessor>());
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
builder.Services.AddScoped<FluentValidationFilter>();
builder.Services.AddScoped<ActionLoggingFilter>();
builder.Services.AddHostedService<DatabaseMigrationHostedService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddMedFundAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services
    .AddControllers(options =>
    {
        options.Filters.AddService<ActionLoggingFilter>();
        options.Filters.AddService<FluentValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new UpperSnakeEnumJsonConverterFactory());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();
app.MapControllers();

app.Run();

static void UseRenderDatabaseUrl(ConfigurationManager configuration)
{
    var databaseUrl = configuration["DATABASE_URL"];
    if (string.IsNullOrWhiteSpace(databaseUrl))
    {
        return;
    }

    configuration["ConnectionStrings:DefaultConnection"] = ToNpgsqlConnectionString(databaseUrl);
}

static string ToNpgsqlConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
    var database = uri.AbsolutePath.TrimStart('/');
    var port = uri.IsDefaultPort ? 5432 : uri.Port;
    var query = ParseDatabaseUrlQuery(uri.Query);
    var sslMode = query.TryGetValue("sslmode", out var configuredSslMode)
        ? ToNpgsqlOptionValue(configuredSslMode)
        : "Require";

    var parts = new List<string>
    {
        $"Host={uri.Host}",
        $"Port={port}",
        $"Database={database}",
        $"Username={username}",
        $"Password={password}",
        $"SSL Mode={sslMode}",
        "Trust Server Certificate=true"
    };

    if (query.TryGetValue("channel_binding", out var channelBinding))
    {
        parts.Add($"Channel Binding={ToNpgsqlOptionValue(channelBinding)}");
    }

    return string.Join(';', parts);
}

static Dictionary<string, string> ParseDatabaseUrlQuery(string query)
{
    return query.TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Split('=', 2))
        .Where(parts => parts.Length == 2)
        .ToDictionary(
            parts => Uri.UnescapeDataString(parts[0]),
            parts => Uri.UnescapeDataString(parts[1]),
            StringComparer.OrdinalIgnoreCase);
}

static string ToNpgsqlOptionValue(string value)
{
    return value.Equals("require", StringComparison.OrdinalIgnoreCase) ? "Require" :
        value.Equals("prefer", StringComparison.OrdinalIgnoreCase) ? "Prefer" :
        value.Equals("disable", StringComparison.OrdinalIgnoreCase) ? "Disable" :
        value;
}

public partial class Program
{
}
