using MedFund.Application.Common;
using MedFund.Domain.Common;
using MedFund.Domain.Entities;
using MedFund.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MedFund.Infrastructure.Persistence;

public sealed class MedFundDbContext : DbContext, IApplicationDbContext
{
    public MedFundDbContext(DbContextOptions<MedFundDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<InsurancePolicy> InsurancePolicies => Set<InsurancePolicy>();

    public DbSet<FinancingRequest> FinancingRequests => Set<FinancingRequest>();

    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();

    public DbSet<Settlement> Settlements => Set<Settlement>();

    public DbSet<EmiScheduleItem> EmiScheduleItems => Set<EmiScheduleItem>();

    public DbSet<DocumentRecord> DocumentRecords => Set<DocumentRecord>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<UserSecurityToken> UserSecurityTokens => Set<UserSecurityToken>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var enumConverter = new EnumToStringConverter<UserRole>();
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasIndex(x => x.Role);
            builder.HasIndex(x => x.OrganizationId);
            builder.HasIndex(x => x.PatientId).IsUnique();
            builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
            builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            builder.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
            builder.Property(x => x.LastName).HasMaxLength(80).IsRequired();
            builder.Property(x => x.PhoneNumber).HasMaxLength(40);
            builder.Property(x => x.ProfilePictureUrl).HasMaxLength(512);
            builder.Property(x => x.Role).HasConversion(enumConverter).HasMaxLength(40).IsRequired();
            builder.HasOne(x => x.Patient).WithOne(x => x.User).HasForeignKey<User>(x => x.PatientId).OnDelete(DeleteBehavior.SetNull);
            builder.HasOne(x => x.Organization).WithMany(x => x.Users).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Organization>(builder =>
        {
            builder.ToTable("organizations");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.Type, x.Name });
            builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
            builder.Property(x => x.City).HasMaxLength(120).IsRequired();
            builder.Property(x => x.RegistrationNumber).HasMaxLength(80);
            builder.Property(x => x.ContactEmail).HasMaxLength(320);
            builder.Property(x => x.ContactPhone).HasMaxLength(40);
        });

        modelBuilder.Entity<Patient>(builder =>
        {
            builder.ToTable("patients");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Email);
            builder.Property(x => x.FullName).HasMaxLength(160).IsRequired();
            builder.Property(x => x.Mobile).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
            builder.Property(x => x.KycStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<InsurancePolicy>(builder =>
        {
            builder.ToTable("insurance_policies");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ProviderName).HasMaxLength(180).IsRequired();
            builder.Property(x => x.PolicyNumberMasked).HasMaxLength(80).IsRequired();
            builder.Property(x => x.SumInsured).HasPrecision(18, 2);
            builder.Property(x => x.ApprovedAmount).HasPrecision(18, 2);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<FinancingRequest>(builder =>
        {
            builder.ToTable("financing_requests");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.CaseNumber).IsUnique();
            builder.HasIndex(x => x.PatientId);
            builder.HasIndex(x => x.HospitalId);
            builder.HasIndex(x => x.InsuranceCompanyId);
            builder.HasIndex(x => x.Status);
            builder.Property(x => x.CaseNumber).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Treatment).HasMaxLength(240).IsRequired();
            builder.Property(x => x.EstimatedBillAmount).HasPrecision(18, 2);
            builder.Property(x => x.InsuranceApprovedAmount).HasPrecision(18, 2);
            builder.Property(x => x.RequestedFinanceAmount).HasPrecision(18, 2);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(60).IsRequired();
            builder.Property(x => x.InsuranceReviewStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<ConsentRecord>(builder =>
        {
            builder.ToTable("consent_records");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Purpose).HasMaxLength(240).IsRequired();
            builder.Property(x => x.AcceptedTermsVersion).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
            builder.Property(x => x.IpAddress).HasMaxLength(80);
            builder.Property(x => x.UserAgent).HasMaxLength(512);
        });

        modelBuilder.Entity<Settlement>(builder =>
        {
            builder.ToTable("settlements");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.FinancingRequestId);
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
            builder.Property(x => x.UtrReference).HasMaxLength(80);
        });

        modelBuilder.Entity<EmiScheduleItem>(builder =>
        {
            builder.ToTable("emi_schedule_items");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<DocumentRecord>(builder =>
        {
            builder.ToTable("document_records");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(60).IsRequired();
            builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            builder.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            builder.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("audit_logs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Action).HasMaxLength(120).IsRequired();
            builder.Property(x => x.EntityType).HasMaxLength(120).IsRequired();
            builder.Property(x => x.EntityId).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.ToTable("refresh_tokens");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
            builder.Ignore(x => x.IsActive);
        });

        modelBuilder.Entity<UserSecurityToken>(builder =>
        {
            builder.ToTable("user_security_tokens");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.HasIndex(x => new { x.UserId, x.Purpose });
            builder.Property(x => x.Purpose).HasMaxLength(80).IsRequired();
            builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            builder.Ignore(x => x.IsActive);
        });
    }
}
