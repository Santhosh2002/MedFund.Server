using MedFund.Application.Common;

namespace MedFund.Api.Security;

public static class PolicyNames
{
    public const string Patient = "Patient";
    public const string Hospital = "Hospital";
    public const string InsuranceCompany = "InsuranceCompany";
}

public static class AuthorizationExtensions
{
    public static IServiceCollection AddMedFundAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.Patient, policy => policy.RequireClaim("role", RoleNames.Patient));
            options.AddPolicy(PolicyNames.Hospital, policy => policy.RequireClaim("role", RoleNames.Hospital));
            options.AddPolicy(PolicyNames.InsuranceCompany, policy => policy.RequireClaim("role", RoleNames.InsuranceCompany));
        });

        return services;
    }
}
