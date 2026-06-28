using MedFund.Domain.Enums;

namespace MedFund.Application.Financing;

public sealed record PatientDto(
    string Id,
    string FullName,
    string Mobile,
    string Email,
    DateOnly? DateOfBirth,
    string KycStatus);

public sealed record OrganizationDto(
    string Id,
    string Type,
    string Name,
    string City,
    string? RegistrationNumber,
    string? ContactEmail,
    string? ContactPhone);

public sealed record InsurancePolicyDto(
    string Id,
    string PatientId,
    string InsuranceCompanyId,
    string ProviderName,
    string PolicyNumberMasked,
    decimal SumInsured,
    decimal ApprovedAmount,
    string Status);

public sealed record FinancingRequestDto(
    string Id,
    string CaseNumber,
    string PatientId,
    string HospitalId,
    string InsuranceCompanyId,
    DateOnly AdmissionDate,
    string Treatment,
    decimal EstimatedBillAmount,
    decimal InsuranceApprovedAmount,
    decimal RequestedFinanceAmount,
    string Status,
    string InsuranceReviewStatus,
    DateTimeOffset? ConsentReceivedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    PatientDto? Patient = null);

public sealed record SettlementDto(
    string Id,
    string FinancingRequestId,
    string HospitalId,
    decimal Amount,
    string Status,
    DateOnly? ExpectedDate,
    DateTimeOffset? SettledAt,
    string? UtrReference);

public sealed record EmiScheduleItemDto(
    string Id,
    string FinancingRequestId,
    DateOnly DueDate,
    decimal Amount,
    string Status,
    DateTimeOffset? PaidAt);

public sealed record DocumentRecordDto(
    string Id,
    string FinancingRequestId,
    string UploadedByUserId,
    string DocumentType,
    string FileName,
    string StorageKey,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAt);

public sealed record PatientDashboardResponse(
    PatientDto Patient,
    FinancingRequestDto? ActiveRequest,
    InsurancePolicyDto? Policy,
    IReadOnlyCollection<EmiScheduleItemDto> EmiSchedule,
    IReadOnlyCollection<FinancingRequestDto> Requests);

public sealed record HospitalDashboardResponse(
    OrganizationDto Hospital,
    IReadOnlyCollection<FinancingRequestDto> Requests,
    IReadOnlyCollection<PatientDto> Patients,
    IReadOnlyCollection<SettlementDto> Settlements);

public sealed record InsuranceDashboardResponse(
    OrganizationDto Company,
    IReadOnlyCollection<FinancingRequestDto> Cases);

public sealed record UpdatePatientProfileRequest(
    string FullName,
    string Mobile,
    string Email,
    DateOnly? DateOfBirth);

public sealed record CreatePatientRequest(
    string FullName,
    string Mobile,
    string Email,
    DateOnly? DateOfBirth);

public sealed record CreateFinancingRequestRequest(
    string PatientId,
    string InsuranceCompanyId,
    DateOnly AdmissionDate,
    string Treatment,
    decimal EstimatedBillAmount,
    decimal InsuranceApprovedAmount,
    decimal RequestedFinanceAmount);

public sealed record CreateFinancingRequestResponse(
    string Id,
    string CaseNumber,
    string Status);

public sealed record ConsentRequest(
    bool Accepted,
    string Purpose,
    string AcceptedTermsVersion);

public sealed record ConsentResponse(
    string ConsentId,
    string Status,
    DateTimeOffset? AcceptedAt);

public sealed record InsuranceDecisionRequest(
    InsuranceReviewStatus ReviewStatus,
    decimal ApprovedAmount,
    string? Notes);

public sealed record InsuranceDecisionResponse(
    string Id,
    string InsuranceReviewStatus,
    decimal InsuranceApprovedAmount,
    DateTimeOffset UpdatedAt);
