using MedFund.Application.Common;
using MedFund.Application.Financing;
using MedFund.Application.Users;
using MedFund.Domain.Entities;

namespace MedFund.Infrastructure.Services;

internal static class MappingExtensions
{
    public static CurrentUserResponse ToCurrentUserResponse(this User user)
    {
        return new CurrentUserResponse(
            user.Id.ToString(),
            $"{user.FirstName} {user.LastName}",
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.ProfilePictureUrl,
            RoleNames.ToApiValue(user.Role),
            user.OrganizationId?.ToString(),
            user.PatientId?.ToString(),
            user.EmailVerifiedAt.HasValue);
    }

    public static PatientDto ToDto(this Patient patient)
    {
        return new PatientDto(
            patient.Id.ToString(),
            patient.FullName,
            patient.Mobile,
            patient.Email,
            patient.DateOfBirth,
            RoleNames.ToUpperSnakeCase(patient.KycStatus.ToString()));
    }

    public static OrganizationDto ToDto(this Organization organization)
    {
        return new OrganizationDto(
            organization.Id.ToString(),
            RoleNames.ToUpperSnakeCase(organization.Type.ToString()),
            organization.Name,
            organization.City,
            organization.RegistrationNumber,
            organization.ContactEmail,
            organization.ContactPhone);
    }

    public static InsurancePolicyDto ToDto(this InsurancePolicy policy)
    {
        return new InsurancePolicyDto(
            policy.Id.ToString(),
            policy.PatientId.ToString(),
            policy.InsuranceCompanyId.ToString(),
            policy.ProviderName,
            policy.PolicyNumberMasked,
            policy.SumInsured,
            policy.ApprovedAmount,
            RoleNames.ToUpperSnakeCase(policy.Status.ToString()));
    }

    public static FinancingRequestDto ToDto(this FinancingRequest request, PatientDto? patient = null)
    {
        return new FinancingRequestDto(
            request.Id.ToString(),
            request.CaseNumber,
            request.PatientId.ToString(),
            request.HospitalId.ToString(),
            request.InsuranceCompanyId.ToString(),
            request.AdmissionDate,
            request.Treatment,
            request.EstimatedBillAmount,
            request.InsuranceApprovedAmount,
            request.RequestedFinanceAmount,
            RoleNames.ToUpperSnakeCase(request.Status.ToString()),
            RoleNames.ToUpperSnakeCase(request.InsuranceReviewStatus.ToString()),
            request.ConsentReceivedAt,
            request.CreatedAt,
            request.UpdatedAt,
            patient);
    }

    public static SettlementDto ToDto(this Settlement settlement)
    {
        return new SettlementDto(
            settlement.Id.ToString(),
            settlement.FinancingRequestId.ToString(),
            settlement.HospitalId.ToString(),
            settlement.Amount,
            RoleNames.ToUpperSnakeCase(settlement.Status.ToString()),
            settlement.ExpectedDate,
            settlement.SettledAt,
            settlement.UtrReference);
    }

    public static EmiScheduleItemDto ToDto(this EmiScheduleItem item)
    {
        return new EmiScheduleItemDto(
            item.Id.ToString(),
            item.FinancingRequestId.ToString(),
            item.DueDate,
            item.Amount,
            RoleNames.ToUpperSnakeCase(item.Status.ToString()),
            item.PaidAt);
    }

    public static DocumentRecordDto ToDto(this DocumentRecord document)
    {
        return new DocumentRecordDto(
            document.Id.ToString(),
            document.FinancingRequestId.ToString(),
            document.UploadedByUserId.ToString(),
            RoleNames.ToUpperSnakeCase(document.DocumentType.ToString()),
            document.FileName,
            document.StorageKey,
            document.ContentType,
            document.SizeBytes,
            document.CreatedAt);
    }
}
