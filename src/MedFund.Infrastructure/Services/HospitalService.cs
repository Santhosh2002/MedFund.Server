using MedFund.Application.Common;
using MedFund.Application.Financing;
using MedFund.Domain.Entities;
using MedFund.Domain.Enums;
using MedFund.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedFund.Infrastructure.Services;

public sealed class HospitalService : ServiceBase, IHospitalService
{
    private readonly IFileStorageService fileStorageService;

    public HospitalService(
        IApplicationDbContext db,
        ICurrentUserAccessor currentUserAccessor,
        IAuditWriter auditWriter,
        IFileStorageService fileStorageService,
        ILogger<HospitalService> logger)
        : base(db, currentUserAccessor, auditWriter, logger)
    {
        this.fileStorageService = fileStorageService;
    }

    public async Task<HospitalDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetDashboardAsync));
        var current = RequireRole(UserRole.Hospital);
        var hospitalId = RequireOrganizationId(current);
        var hospital = await Db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == hospitalId, cancellationToken)
            ?? throw new NotFoundException("Hospital was not found.");
        var requests = await HospitalRequests(hospitalId).OrderByDescending(x => x.CreatedAt).Take(25).ToListAsync(cancellationToken);
        var patientIds = requests.Select(x => x.PatientId).Distinct().ToArray();
        var patients = await Db.Patients.AsNoTracking().Where(x => patientIds.Contains(x.Id)).ToListAsync(cancellationToken);
        var requestIds = requests.Select(x => x.Id).ToArray();
        var settlements = await Db.Settlements.AsNoTracking().Where(x => requestIds.Contains(x.FinancingRequestId)).ToListAsync(cancellationToken);
        LogKey(nameof(GetDashboardAsync), "loaded hospital dashboard", new { HospitalId = hospitalId, RequestCount = requests.Count, PatientCount = patients.Count, SettlementCount = settlements.Count });

        return new HospitalDashboardResponse(
            hospital.ToDto(),
            requests.Select(x => x.ToDto()).ToArray(),
            patients.Select(x => x.ToDto()).ToArray(),
            settlements.Select(x => x.ToDto()).ToArray());
    }

    public async Task<PatientDto> CreateOrLinkPatientAsync(CreatePatientRequest request, CancellationToken cancellationToken)
    {
        LogReceived(nameof(CreateOrLinkPatientAsync), new { request.FullName, request.Mobile, request.Email, request.DateOfBirth });
        var current = RequireRole(UserRole.Hospital);
        _ = RequireOrganizationId(current);
        var patient = await Db.Patients.FirstOrDefaultAsync(x => x.Email == request.Email || x.Mobile == request.Mobile, cancellationToken);
        if (patient is null)
        {
            patient = new Patient
            {
                FullName = request.FullName,
                Mobile = request.Mobile,
                Email = request.Email,
                DateOfBirth = request.DateOfBirth
            };
            Db.Patients.Add(patient);
            AuditWriter.Add(current.UserId, "PatientCreated", nameof(Patient), patient.Id.ToString());
        }

        await Db.SaveChangesAsync(cancellationToken);
        LogKey(nameof(CreateOrLinkPatientAsync), "created or linked patient", new { patient.Id, patient.Email });
        return patient.ToDto();
    }

    public async Task<IReadOnlyCollection<PatientDto>> GetPatientsAsync(CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetPatientsAsync));
        var current = RequireRole(UserRole.Hospital);
        var hospitalId = RequireOrganizationId(current);
        var patientIds = HospitalRequests(hospitalId).Select(x => x.PatientId).Distinct();
        var patients = await Db.Patients.AsNoTracking().Where(x => patientIds.Contains(x.Id)).OrderBy(x => x.FullName).ToListAsync(cancellationToken);
        LogKey(nameof(GetPatientsAsync), "loaded hospital patients", new { HospitalId = hospitalId, Count = patients.Count });
        return patients.Select(x => x.ToDto()).ToArray();
    }

    public async Task<PatientDto> GetPatientAsync(Guid id, CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetPatientAsync), new { Id = id });
        var current = RequireRole(UserRole.Hospital);
        var hospitalId = RequireOrganizationId(current);
        var isLinked = await HospitalRequests(hospitalId).AnyAsync(x => x.PatientId == id, cancellationToken);
        if (!isLinked)
        {
            throw new NotFoundException("Patient was not found for this hospital.");
        }

        var patient = await Db.Patients.AsNoTracking().FirstAsync(x => x.Id == id, cancellationToken);
        return patient.ToDto();
    }

    public async Task<CreateFinancingRequestResponse> CreateFinancingRequestAsync(CreateFinancingRequestRequest request, CancellationToken cancellationToken)
    {
        LogReceived(nameof(CreateFinancingRequestAsync), new { request.PatientId, request.InsuranceCompanyId, request.AdmissionDate, request.Treatment, request.EstimatedBillAmount, request.InsuranceApprovedAmount, request.RequestedFinanceAmount });
        var current = RequireRole(UserRole.Hospital);
        var hospitalId = RequireOrganizationId(current);
        var patientId = ParseId(request.PatientId, nameof(request.PatientId));
        var insuranceCompanyId = ParseId(request.InsuranceCompanyId, nameof(request.InsuranceCompanyId));

        _ = await Db.Patients.FirstOrDefaultAsync(x => x.Id == patientId, cancellationToken)
            ?? throw new NotFoundException("Patient was not found.");
        _ = await Db.Organizations.FirstOrDefaultAsync(x => x.Id == insuranceCompanyId && x.Type == OrganizationType.InsuranceCompany, cancellationToken)
            ?? throw new NotFoundException("Insurance company was not found.");

        var count = await Db.FinancingRequests.CountAsync(cancellationToken);
        var financingRequest = new FinancingRequest
        {
            CaseNumber = $"MF-{DateTime.UtcNow.Year}-{count + 1:0000}",
            PatientId = patientId,
            HospitalId = hospitalId,
            InsuranceCompanyId = insuranceCompanyId,
            AdmissionDate = request.AdmissionDate,
            Treatment = request.Treatment,
            EstimatedBillAmount = request.EstimatedBillAmount,
            InsuranceApprovedAmount = request.InsuranceApprovedAmount,
            RequestedFinanceAmount = request.RequestedFinanceAmount,
            CreatedByUserId = current.UserId
        };

        Db.FinancingRequests.Add(financingRequest);
        AuditWriter.Add(current.UserId, "FinancingRequestCreated", nameof(FinancingRequest), financingRequest.Id.ToString());
        await Db.SaveChangesAsync(cancellationToken);
        LogKey(nameof(CreateFinancingRequestAsync), "created financing request", new { financingRequest.Id, financingRequest.CaseNumber, financingRequest.Status });

        return new CreateFinancingRequestResponse(financingRequest.Id.ToString(), financingRequest.CaseNumber, RoleNames.ToUpperSnakeCase(financingRequest.Status.ToString()));
    }

    public async Task<PagedResult<FinancingRequestDto>> GetFinancingRequestsAsync(
        FinancingRequestStatus? status,
        Guid? patientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetFinancingRequestsAsync), new { status, patientId, fromDate, toDate, page, pageSize });
        var current = RequireRole(UserRole.Hospital);
        var hospitalId = RequireOrganizationId(current);
        var query = HospitalRequests(hospitalId);
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (patientId.HasValue)
        {
            query = query.Where(x => x.PatientId == patientId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.AdmissionDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.AdmissionDate <= toDate.Value);
        }

        return await ToPageAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<FinancingRequestDto> GetFinancingRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetFinancingRequestAsync), new { Id = id });
        var current = RequireRole(UserRole.Hospital);
        var hospitalId = RequireOrganizationId(current);
        var request = await HospitalRequests(hospitalId).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Financing request was not found.");

        return request.ToDto();
    }

    public async Task<DocumentRecordDto> AddDocumentAsync(Guid financingRequestId, DocumentType documentType, FileUploadDescriptor file, CancellationToken cancellationToken)
    {
        LogReceived(nameof(AddDocumentAsync), new { FinancingRequestId = financingRequestId, DocumentType = documentType, file.FileName, file.ContentType, file.SizeBytes });
        var current = RequireRole(UserRole.Hospital);
        var hospitalId = RequireOrganizationId(current);
        var request = await Db.FinancingRequests.FirstOrDefaultAsync(x => x.Id == financingRequestId && x.HospitalId == hospitalId, cancellationToken)
            ?? throw new NotFoundException("Financing request was not found.");

        var storedFile = await fileStorageService.SaveAsync($"financing-requests/{request.Id}", file, cancellationToken);
        var document = new DocumentRecord
        {
            FinancingRequestId = request.Id,
            UploadedByUserId = current.UserId,
            DocumentType = documentType,
            FileName = storedFile.FileName,
            ContentType = storedFile.ContentType,
            SizeBytes = storedFile.SizeBytes,
            StorageKey = storedFile.StorageKey
        };

        Db.DocumentRecords.Add(document);
        AuditWriter.Add(current.UserId, "DocumentUploaded", nameof(FinancingRequest), request.Id.ToString());
        await Db.SaveChangesAsync(cancellationToken);
        LogKey(nameof(AddDocumentAsync), "stored document metadata", new { DocumentId = document.Id, FinancingRequestId = request.Id, document.StorageKey });
        return document.ToDto();
    }

    public async Task<CreateFinancingRequestResponse> SubmitAsync(Guid financingRequestId, CancellationToken cancellationToken)
    {
        LogReceived(nameof(SubmitAsync), new { FinancingRequestId = financingRequestId });
        var current = RequireRole(UserRole.Hospital);
        var hospitalId = RequireOrganizationId(current);
        var request = await Db.FinancingRequests.FirstOrDefaultAsync(x => x.Id == financingRequestId && x.HospitalId == hospitalId, cancellationToken)
            ?? throw new NotFoundException("Financing request was not found.");

        FinancingRequestStateMachine.EnsureCanTransition(request.Status, FinancingRequestStatus.AwaitingPatientConsent);
        request.Status = FinancingRequestStatus.AwaitingPatientConsent;
        AuditWriter.Add(current.UserId, "FinancingRequestSubmitted", nameof(FinancingRequest), request.Id.ToString());
        await Db.SaveChangesAsync(cancellationToken);
        LogKey(nameof(SubmitAsync), "submitted financing request", new { request.Id, request.CaseNumber, request.Status });

        return new CreateFinancingRequestResponse(request.Id.ToString(), request.CaseNumber, RoleNames.ToUpperSnakeCase(request.Status.ToString()));
    }

    public async Task<IReadOnlyCollection<SettlementDto>> GetSettlementsAsync(CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetSettlementsAsync));
        var current = RequireRole(UserRole.Hospital);
        var hospitalId = RequireOrganizationId(current);
        var settlements = await Db.Settlements.AsNoTracking()
            .Where(x => x.HospitalId == hospitalId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return settlements.Select(x => x.ToDto()).ToArray();
    }

    private IQueryable<FinancingRequest> HospitalRequests(Guid hospitalId)
    {
        return Db.FinancingRequests.AsNoTracking().Where(x => x.HospitalId == hospitalId);
    }

    private static Guid RequireOrganizationId(CurrentUser current)
    {
        return current.OrganizationId ?? throw new ForbiddenException("Organization context is missing.");
    }

    private static async Task<PagedResult<FinancingRequestDto>> ToPageAsync(IQueryable<FinancingRequest> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<FinancingRequestDto>(items.Select(x => x.ToDto()).ToArray(), page, pageSize, total);
    }
}
