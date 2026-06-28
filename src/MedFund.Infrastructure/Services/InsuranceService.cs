using MedFund.Application.Common;
using MedFund.Application.Financing;
using MedFund.Domain.Entities;
using MedFund.Domain.Enums;
using MedFund.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedFund.Infrastructure.Services;

public sealed class InsuranceService : ServiceBase, IInsuranceService
{
    public InsuranceService(IApplicationDbContext db, ICurrentUserAccessor currentUserAccessor, IAuditWriter auditWriter, ILogger<InsuranceService> logger)
        : base(db, currentUserAccessor, auditWriter, logger)
    {
    }

    public async Task<InsuranceDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetDashboardAsync));
        var current = RequireRole(UserRole.InsuranceCompany);
        var companyId = RequireOrganizationId(current);
        var company = await Db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == companyId, cancellationToken)
            ?? throw new NotFoundException("Insurance company was not found.");
        var cases = await InsuranceCases(companyId)
            .Include(x => x.Patient)
            .OrderByDescending(x => x.CreatedAt)
            .Take(25)
            .ToListAsync(cancellationToken);
        LogKey(nameof(GetDashboardAsync), "loaded insurance dashboard", new { CompanyId = companyId, CaseCount = cases.Count });

        return new InsuranceDashboardResponse(
            company.ToDto(),
            cases.Select(x => x.ToDto(x.Patient?.ToDto())).ToArray());
    }

    public async Task<PagedResult<FinancingRequestDto>> GetCasesAsync(
        InsuranceReviewStatus? reviewStatus,
        FinancingRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetCasesAsync), new { reviewStatus, status, page, pageSize });
        var current = RequireRole(UserRole.InsuranceCompany);
        var companyId = RequireOrganizationId(current);
        IQueryable<FinancingRequest> query = InsuranceCases(companyId).Include(x => x.Patient);
        if (reviewStatus.HasValue)
        {
            query = query.Where(x => x.InsuranceReviewStatus == reviewStatus.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        LogKey(nameof(GetCasesAsync), "loaded insurance cases page", new { CompanyId = companyId, total, Returned = items.Count, page, pageSize });
        return new PagedResult<FinancingRequestDto>(items.Select(x => x.ToDto(x.Patient?.ToDto())).ToArray(), page, pageSize, total);
    }

    public async Task<FinancingRequestDto> GetCaseAsync(Guid id, CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetCaseAsync), new { Id = id });
        var current = RequireRole(UserRole.InsuranceCompany);
        var companyId = RequireOrganizationId(current);
        var request = await InsuranceCases(companyId).Include(x => x.Patient).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Case was not found.");

        return request.ToDto(request.Patient?.ToDto());
    }

    public async Task<InsuranceDecisionResponse> DecideAsync(Guid id, InsuranceDecisionRequest request, CancellationToken cancellationToken)
    {
        LogReceived(nameof(DecideAsync), new { Id = id, request.ReviewStatus, request.ApprovedAmount, HasNotes = !string.IsNullOrWhiteSpace(request.Notes) });
        var current = RequireRole(UserRole.InsuranceCompany);
        var companyId = RequireOrganizationId(current);
        var financingRequest = await Db.FinancingRequests.FirstOrDefaultAsync(x => x.Id == id && x.InsuranceCompanyId == companyId, cancellationToken)
            ?? throw new NotFoundException("Case was not found.");

        if (request.ApprovedAmount > financingRequest.EstimatedBillAmount)
        {
            throw new MedFundException("Approved amount cannot exceed estimated bill amount.");
        }

        if (financingRequest.Status == FinancingRequestStatus.ConsentReceived)
        {
            FinancingRequestStateMachine.EnsureCanTransition(financingRequest.Status, FinancingRequestStatus.InsuranceReview);
            financingRequest.Status = FinancingRequestStatus.InsuranceReview;
        }

        if (financingRequest.Status != FinancingRequestStatus.InsuranceReview)
        {
            throw new ConflictException("Case is not ready for insurance decision.");
        }

        financingRequest.InsuranceReviewStatus = request.ReviewStatus;
        financingRequest.InsuranceApprovedAmount = request.ApprovedAmount;
        if (request.ReviewStatus == InsuranceReviewStatus.Approved)
        {
            FinancingRequestStateMachine.EnsureCanTransition(financingRequest.Status, FinancingRequestStatus.Approved);
            financingRequest.Status = FinancingRequestStatus.Approved;
        }
        else if (request.ReviewStatus == InsuranceReviewStatus.Rejected)
        {
            FinancingRequestStateMachine.EnsureCanTransition(financingRequest.Status, FinancingRequestStatus.Rejected);
            financingRequest.Status = FinancingRequestStatus.Rejected;
        }

        AuditWriter.Add(current.UserId, "InsuranceDecisionSubmitted", nameof(FinancingRequest), financingRequest.Id.ToString(), afterJson: request.Notes);
        await Db.SaveChangesAsync(cancellationToken);
        LogKey(nameof(DecideAsync), "saved insurance decision", new { financingRequest.Id, financingRequest.Status, financingRequest.InsuranceReviewStatus, financingRequest.InsuranceApprovedAmount });

        return new InsuranceDecisionResponse(
            financingRequest.Id.ToString(),
            RoleNames.ToUpperSnakeCase(financingRequest.InsuranceReviewStatus.ToString()),
            financingRequest.InsuranceApprovedAmount,
            financingRequest.UpdatedAt);
    }

    public async Task<IReadOnlyCollection<SettlementDto>> GetSettlementsAsync(CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetSettlementsAsync));
        var current = RequireRole(UserRole.InsuranceCompany);
        var companyId = RequireOrganizationId(current);
        var requestIds = InsuranceCases(companyId).Select(x => x.Id);
        var settlements = await Db.Settlements.AsNoTracking()
            .Where(x => requestIds.Contains(x.FinancingRequestId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        LogKey(nameof(GetSettlementsAsync), "loaded insurance settlements", new { CompanyId = companyId, Count = settlements.Count });

        return settlements.Select(x => x.ToDto()).ToArray();
    }

    private IQueryable<FinancingRequest> InsuranceCases(Guid companyId)
    {
        return Db.FinancingRequests.AsNoTracking().Where(x => x.InsuranceCompanyId == companyId);
    }

    private static Guid RequireOrganizationId(CurrentUser current)
    {
        return current.OrganizationId ?? throw new ForbiddenException("Organization context is missing.");
    }
}
