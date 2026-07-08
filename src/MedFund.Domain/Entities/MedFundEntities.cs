using MedFund.Domain.Common;
using MedFund.Domain.Enums;

namespace MedFund.Domain.Entities;

public sealed class User : AuditableEntity
{
    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public UserRole Role { get; set; }

    public Guid? OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public Guid? PatientId { get; set; }

    public Patient? Patient { get; set; }

    public DateTimeOffset? EmailVerifiedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RefreshToken> RefreshTokens { get; } = new List<RefreshToken>();

    public ICollection<UserSecurityToken> SecurityTokens { get; } = new List<UserSecurityToken>();
}

public sealed class Organization : AuditableEntity
{
    public OrganizationType Type { get; set; }

    public required string Name { get; set; }

    public required string City { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<User> Users { get; } = new List<User>();
}

public sealed class Patient : AuditableEntity
{
    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public required string FullName { get; set; }

    public required string Mobile { get; set; }

    public required string Email { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public KycStatus KycStatus { get; set; } = KycStatus.Pending;

    public ICollection<FinancingRequest> FinancingRequests { get; } = new List<FinancingRequest>();
}

public sealed class InsurancePolicy : AuditableEntity
{
    public Guid PatientId { get; set; }

    public Patient? Patient { get; set; }

    public Guid InsuranceCompanyId { get; set; }

    public Organization? InsuranceCompany { get; set; }

    public required string ProviderName { get; set; }

    public required string PolicyNumberMasked { get; set; }

    public decimal SumInsured { get; set; }

    public decimal ApprovedAmount { get; set; }

    public InsurancePolicyStatus Status { get; set; } = InsurancePolicyStatus.Active;
}

public sealed class FinancingRequest : AuditableEntity
{
    public required string CaseNumber { get; set; }

    public Guid PatientId { get; set; }

    public Patient? Patient { get; set; }

    public Guid HospitalId { get; set; }

    public Organization? Hospital { get; set; }

    public Guid InsuranceCompanyId { get; set; }

    public Organization? InsuranceCompany { get; set; }

    public DateOnly AdmissionDate { get; set; }

    public required string Treatment { get; set; }

    public decimal EstimatedBillAmount { get; set; }

    public decimal InsuranceApprovedAmount { get; set; }

    public decimal RequestedFinanceAmount { get; set; }

    public FinancingRequestStatus Status { get; set; } = FinancingRequestStatus.Draft;

    public InsuranceReviewStatus InsuranceReviewStatus { get; set; } = InsuranceReviewStatus.Pending;

    public DateTimeOffset? ConsentReceivedAt { get; set; }

    public Guid CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public ICollection<ConsentRecord> ConsentRecords { get; } = new List<ConsentRecord>();

    public ICollection<DocumentRecord> Documents { get; } = new List<DocumentRecord>();

    public ICollection<EmiScheduleItem> EmiSchedule { get; } = new List<EmiScheduleItem>();
}

public sealed class ConsentRecord : AuditableEntity
{
    public Guid PatientId { get; set; }

    public Patient? Patient { get; set; }

    public Guid FinancingRequestId { get; set; }

    public FinancingRequest? FinancingRequest { get; set; }

    public required string Purpose { get; set; }

    public required string AcceptedTermsVersion { get; set; }

    public ConsentStatus Status { get; set; } = ConsentStatus.Pending;

    public DateTimeOffset? AcceptedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}

public sealed class Settlement : AuditableEntity
{
    public Guid FinancingRequestId { get; set; }

    public FinancingRequest? FinancingRequest { get; set; }

    public Guid HospitalId { get; set; }

    public Organization? Hospital { get; set; }

    public decimal Amount { get; set; }

    public SettlementStatus Status { get; set; } = SettlementStatus.Pending;

    public DateOnly? ExpectedDate { get; set; }

    public DateTimeOffset? SettledAt { get; set; }

    public string? UtrReference { get; set; }
}

public sealed class EmiScheduleItem : AuditableEntity
{
    public Guid FinancingRequestId { get; set; }

    public FinancingRequest? FinancingRequest { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal Amount { get; set; }

    public EmiScheduleStatus Status { get; set; } = EmiScheduleStatus.Pending;

    public DateTimeOffset? PaidAt { get; set; }
}

public sealed class DocumentRecord : AuditableEntity
{
    public Guid FinancingRequestId { get; set; }

    public FinancingRequest? FinancingRequest { get; set; }

    public Guid UploadedByUserId { get; set; }

    public User? UploadedByUser { get; set; }

    public DocumentType DocumentType { get; set; } = DocumentType.Other;

    public required string FileName { get; set; }

    public required string StorageKey { get; set; }

    public required string ContentType { get; set; }

    public long SizeBytes { get; set; }
}

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? ActorUserId { get; set; }

    public required string Action { get; set; }

    public required string EntityType { get; set; }

    public required string EntityId { get; set; }

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public string? IpAddress { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class RefreshToken : AuditableEntity
{
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public required string TokenHash { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}

public sealed class UserSecurityToken : AuditableEntity
{
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public required string Purpose { get; set; }

    public required string TokenHash { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }

    public bool IsActive => ConsumedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}

public sealed class PartnershipLead : AuditableEntity
{
    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string PhoneNumber { get; set; }

    public required string Email { get; set; }

    public PartnershipPartnerType PartnerType { get; set; }

    public required string OrganizationName { get; set; }

    public PartnershipLeadStatus Status { get; set; } = PartnershipLeadStatus.New;

    public string Source { get; set; } = "WEBSITE_PARTNERSHIP_FORM";

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset? ContactedAt { get; set; }
}
