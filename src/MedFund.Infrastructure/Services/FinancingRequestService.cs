using MedFund.Application.Common;
using MedFund.Application.Financing;
using MedFund.Domain.Entities;
using MedFund.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedFund.Infrastructure.Services;

public sealed class FinancingRequestService : ServiceBase, IFinancingRequestService
{
    public FinancingRequestService(IApplicationDbContext db, ICurrentUserAccessor currentUserAccessor, IAuditWriter auditWriter, ILogger<FinancingRequestService> logger)
        : base(db, currentUserAccessor, auditWriter, logger)
    {
    }

    public async Task<PagedResult<FinancingRequestDto>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetAllAsync), new { page, pageSize });
        var query = ScopedRequests(CurrentUser).Include(x => x.Patient);
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        LogKey(nameof(GetAllAsync), "loaded scoped financing requests page", new { total, Returned = items.Count, page, pageSize });

        return new PagedResult<FinancingRequestDto>(items.Select(x => x.ToDto(x.Patient?.ToDto())).ToArray(), page, pageSize, total);
    }

    public async Task<FinancingRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetByIdAsync), new { Id = id });
        var request = await ScopedRequests(CurrentUser).Include(x => x.Patient).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Financing request was not found.");

        return request.ToDto(request.Patient?.ToDto());
    }

    private IQueryable<FinancingRequest> ScopedRequests(CurrentUser current)
    {
        var role = RoleNames.ParseRole(current.Role);
        var query = Db.FinancingRequests.AsNoTracking();

        return role switch
        {
            UserRole.Patient when current.PatientId.HasValue => query.Where(x => x.PatientId == current.PatientId.Value),
            UserRole.Hospital when current.OrganizationId.HasValue => query.Where(x => x.HospitalId == current.OrganizationId.Value),
            UserRole.InsuranceCompany when current.OrganizationId.HasValue => query.Where(x => x.InsuranceCompanyId == current.OrganizationId.Value),
            _ => throw new ForbiddenException("The current user does not have a financing request scope.")
        };
    }
}
