using MedFund.Application.Common;
using MedFund.Infrastructure.Auth;
using MedFund.Infrastructure.Email;
using MedFund.Infrastructure.Persistence;
using MedFund.Infrastructure.Services;
using MedFund.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedFund.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<MailjetOptions>(configuration.GetSection(MailjetOptions.SectionName));

        services.AddDbContext<MedFundDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<MedFundDbContext>());
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddHttpClient<IEmailSender, MailjetEmailSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.mailjet.com/");
        });
        services.AddScoped<IDevelopmentDataSeeder, DevelopmentDataSeeder>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IHospitalService, HospitalService>();
        services.AddScoped<IInsuranceService, InsuranceService>();
        services.AddScoped<IFinancingRequestService, FinancingRequestService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<IPartnershipService, PartnershipService>();

        return services;
    }
}
