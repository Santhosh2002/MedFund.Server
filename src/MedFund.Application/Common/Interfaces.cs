using MedFund.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedFund.Application.Common;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<Organization> Organizations { get; }

    DbSet<Patient> Patients { get; }

    DbSet<InsurancePolicy> InsurancePolicies { get; }

    DbSet<FinancingRequest> FinancingRequests { get; }

    DbSet<ConsentRecord> ConsentRecords { get; }

    DbSet<Settlement> Settlements { get; }

    DbSet<EmiScheduleItem> EmiScheduleItems { get; }

    DbSet<DocumentRecord> DocumentRecords { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<UserSecurityToken> UserSecurityTokens { get; }

    DbSet<PartnershipLead> PartnershipLeads { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ICurrentUserAccessor
{
    CurrentUser GetRequiredCurrentUser();
}

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string hash);
}

public interface IJwtTokenService
{
    string CreateAccessToken(User user);

    string CreateRefreshToken();

    string HashToken(string token);
}

public interface IAuditWriter
{
    void Add(Guid? actorUserId, string action, string entityType, string entityId, string? beforeJson = null, string? afterJson = null, string? ipAddress = null);
}

public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(string folder, FileUploadDescriptor file, CancellationToken cancellationToken);
}

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
