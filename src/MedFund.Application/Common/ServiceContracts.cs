using MedFund.Application.Auth;
using MedFund.Application.Financing;
using MedFund.Application.Users;
using MedFund.Domain.Enums;

namespace MedFund.Application.Common;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> SignupAsync(SignupRequest request, CancellationToken cancellationToken);

    Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task AcceptForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);

    Task VerifyEmailAsync(string token, CancellationToken cancellationToken);

    Task ResendVerificationAsync(ResendVerificationRequest request, CancellationToken cancellationToken);
}

public interface IUserService
{
    Task<CurrentUserResponse> GetCurrentAsync(CancellationToken cancellationToken);

    Task<CurrentUserResponse> UpdateCurrentAsync(UpdateCurrentUserRequest request, CancellationToken cancellationToken);
}

public interface IPatientService
{
    Task<PatientDto> GetMeAsync(CancellationToken cancellationToken);

    Task<PatientDto> UpdateMeAsync(UpdatePatientProfileRequest request, CancellationToken cancellationToken);

    Task<PatientDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FinancingRequestDto>> GetFinancingRequestsAsync(CancellationToken cancellationToken);

    Task<FinancingRequestDto> GetFinancingRequestAsync(Guid id, CancellationToken cancellationToken);

    Task<ConsentResponse> ConsentAsync(Guid financingRequestId, ConsentRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EmiScheduleItemDto>> GetRepaymentsAsync(CancellationToken cancellationToken);
}

public interface IHospitalService
{
    Task<HospitalDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);

    Task<PatientDto> CreateOrLinkPatientAsync(CreatePatientRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PatientDto>> GetPatientsAsync(CancellationToken cancellationToken);

    Task<PatientDto> GetPatientAsync(Guid id, CancellationToken cancellationToken);

    Task<CreateFinancingRequestResponse> CreateFinancingRequestAsync(CreateFinancingRequestRequest request, CancellationToken cancellationToken);

    Task<PagedResult<FinancingRequestDto>> GetFinancingRequestsAsync(
        FinancingRequestStatus? status,
        Guid? patientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<FinancingRequestDto> GetFinancingRequestAsync(Guid id, CancellationToken cancellationToken);

    Task<DocumentRecordDto> AddDocumentAsync(Guid financingRequestId, DocumentType documentType, FileUploadDescriptor file, CancellationToken cancellationToken);

    Task<CreateFinancingRequestResponse> SubmitAsync(Guid financingRequestId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SettlementDto>> GetSettlementsAsync(CancellationToken cancellationToken);
}

public interface IInsuranceService
{
    Task<InsuranceDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);

    Task<PagedResult<FinancingRequestDto>> GetCasesAsync(InsuranceReviewStatus? reviewStatus, FinancingRequestStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    Task<FinancingRequestDto> GetCaseAsync(Guid id, CancellationToken cancellationToken);

    Task<InsuranceDecisionResponse> DecideAsync(Guid id, InsuranceDecisionRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SettlementDto>> GetSettlementsAsync(CancellationToken cancellationToken);
}

public interface IFinancingRequestService
{
    Task<PagedResult<FinancingRequestDto>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<FinancingRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

public interface ISettlementService
{
    Task<IReadOnlyCollection<SettlementDto>> GetScopedAsync(CancellationToken cancellationToken);

    Task<SettlementDto> GetScopedByIdAsync(Guid id, CancellationToken cancellationToken);
}
