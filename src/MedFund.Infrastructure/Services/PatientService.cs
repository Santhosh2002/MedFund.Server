using MedFund.Application.Common;
using MedFund.Application.Financing;
using MedFund.Domain.Entities;
using MedFund.Domain.Enums;
using MedFund.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedFund.Infrastructure.Services;

public sealed class PatientService : ServiceBase, IPatientService
{
    public PatientService(IApplicationDbContext db, ICurrentUserAccessor currentUserAccessor, IAuditWriter auditWriter, ILogger<PatientService> logger)
        : base(db, currentUserAccessor, auditWriter, logger)
    {
    }

    public async Task<PatientDto> GetMeAsync(CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetMeAsync));
        var current = RequireRole(UserRole.Patient);
        var patient = await LoadCurrentPatientAsync(current, cancellationToken);
        LogKey(nameof(GetMeAsync), "loaded patient profile", new { patient.Id, current.UserId });
        return patient.ToDto();
    }

    public async Task<PatientDto> UpdateMeAsync(UpdatePatientProfileRequest request, CancellationToken cancellationToken)
    {
        LogReceived(nameof(UpdateMeAsync), new { request.FullName, request.Mobile, request.Email, request.DateOfBirth });
        var current = RequireRole(UserRole.Patient);
        var patient = await LoadCurrentPatientAsync(current, cancellationToken);
        patient.FullName = request.FullName;
        patient.Mobile = request.Mobile;
        patient.Email = request.Email;
        patient.DateOfBirth = request.DateOfBirth;
        AuditWriter.Add(current.UserId, "PatientUpdated", nameof(Patient), patient.Id.ToString());
        await Db.SaveChangesAsync(cancellationToken);
        LogKey(nameof(UpdateMeAsync), "updated patient profile", new { patient.Id, current.UserId });
        return patient.ToDto();
    }

    public async Task<PatientDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetDashboardAsync));
        var current = RequireRole(UserRole.Patient);
        var patient = await LoadCurrentPatientAsync(current, cancellationToken);
        var requests = await PatientRequests(patient.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var activeRequest = requests.FirstOrDefault(x => x.Status is not FinancingRequestStatus.Settled and not FinancingRequestStatus.Cancelled and not FinancingRequestStatus.Rejected);
        var policy = await Db.InsurancePolicies.AsNoTracking()
            .Where(x => x.PatientId == patient.Id && x.Status == InsurancePolicyStatus.Active)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var emiSchedule = activeRequest is null
            ? new List<EmiScheduleItem>()
            : await Db.EmiScheduleItems.AsNoTracking()
                .Where(x => x.FinancingRequestId == activeRequest.Id)
                .OrderBy(x => x.DueDate)
                .ToListAsync(cancellationToken);

        return new PatientDashboardResponse(
            patient.ToDto(),
            activeRequest?.ToDto(),
            policy?.ToDto(),
            emiSchedule.Select(x => x.ToDto()).ToArray(),
            requests.Select(x => x.ToDto()).ToArray());
    }

    public async Task<IReadOnlyCollection<FinancingRequestDto>> GetFinancingRequestsAsync(CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetFinancingRequestsAsync));
        var current = RequireRole(UserRole.Patient);
        var patientId = current.PatientId ?? throw new ForbiddenException("Patient context is missing.");
        var requests = await PatientRequests(patientId).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        LogKey(nameof(GetFinancingRequestsAsync), "loaded patient financing requests", new { PatientId = patientId, Count = requests.Count });
        return requests.Select(x => x.ToDto()).ToArray();
    }

    public async Task<FinancingRequestDto> GetFinancingRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetFinancingRequestAsync), new { Id = id });
        var current = RequireRole(UserRole.Patient);
        var patientId = current.PatientId ?? throw new ForbiddenException("Patient context is missing.");
        var request = await PatientRequests(patientId).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Financing request was not found.");

        return request.ToDto();
    }

    public async Task<ConsentResponse> ConsentAsync(Guid financingRequestId, ConsentRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        LogReceived(nameof(ConsentAsync), new { FinancingRequestId = financingRequestId, request.Accepted, request.Purpose, request.AcceptedTermsVersion, IpAddress = ipAddress, UserAgent = userAgent });
        var current = RequireRole(UserRole.Patient);
        var patientId = current.PatientId ?? throw new ForbiddenException("Patient context is missing.");
        var financingRequest = await Db.FinancingRequests
            .FirstOrDefaultAsync(x => x.Id == financingRequestId && x.PatientId == patientId, cancellationToken)
            ?? throw new NotFoundException("Financing request was not found.");

        if (await Db.ConsentRecords.AnyAsync(x => x.FinancingRequestId == financingRequestId && x.Status == ConsentStatus.Accepted, cancellationToken))
        {
            throw new ConflictException("Consent has already been accepted for this request.");
        }

        FinancingRequestStateMachine.EnsureCanTransition(financingRequest.Status, FinancingRequestStatus.ConsentReceived);

        var consent = new ConsentRecord
        {
            PatientId = patientId,
            FinancingRequestId = financingRequestId,
            Purpose = request.Purpose,
            AcceptedTermsVersion = request.AcceptedTermsVersion,
            Status = ConsentStatus.Accepted,
            AcceptedAt = DateTimeOffset.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
        financingRequest.Status = FinancingRequestStatus.ConsentReceived;
        financingRequest.ConsentReceivedAt = consent.AcceptedAt;
        Db.ConsentRecords.Add(consent);
        AuditWriter.Add(current.UserId, "PatientConsentAccepted", nameof(FinancingRequest), financingRequest.Id.ToString(), ipAddress: ipAddress);
        await Db.SaveChangesAsync(cancellationToken);
        LogKey(nameof(ConsentAsync), "accepted patient consent", new { financingRequest.Id, ConsentId = consent.Id, financingRequest.Status });

        return new ConsentResponse(consent.Id.ToString(), RoleNames.ToUpperSnakeCase(consent.Status.ToString()), consent.AcceptedAt);
    }

    public async Task<IReadOnlyCollection<EmiScheduleItemDto>> GetRepaymentsAsync(CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetRepaymentsAsync));
        var current = RequireRole(UserRole.Patient);
        var patientId = current.PatientId ?? throw new ForbiddenException("Patient context is missing.");
        var requestIds = PatientRequests(patientId).Select(x => x.Id);
        var items = await Db.EmiScheduleItems.AsNoTracking()
            .Where(x => requestIds.Contains(x.FinancingRequestId))
            .OrderBy(x => x.DueDate)
            .ToListAsync(cancellationToken);

        return items.Select(x => x.ToDto()).ToArray();
    }

    private IQueryable<FinancingRequest> PatientRequests(Guid patientId)
    {
        return Db.FinancingRequests.AsNoTracking().Where(x => x.PatientId == patientId);
    }

    private async Task<Patient> LoadCurrentPatientAsync(CurrentUser current, CancellationToken cancellationToken)
    {
        var patientId = current.PatientId ?? throw new ForbiddenException("Patient context is missing.");
        return await Db.Patients.FirstOrDefaultAsync(x => x.Id == patientId, cancellationToken)
            ?? throw new NotFoundException("Patient profile was not found.");
    }
}
