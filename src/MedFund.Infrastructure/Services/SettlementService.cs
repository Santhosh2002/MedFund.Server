using MedFund.Application.Common;
using MedFund.Application.Financing;
using MedFund.Domain.Entities;
using MedFund.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedFund.Infrastructure.Services;

public sealed class SettlementService : ServiceBase, ISettlementService
{
    public SettlementService(IApplicationDbContext db, ICurrentUserAccessor currentUserAccessor, IAuditWriter auditWriter, ILogger<SettlementService> logger)
        : base(db, currentUserAccessor, auditWriter, logger)
    {
    }

    public async Task<IReadOnlyCollection<SettlementDto>> GetScopedAsync(CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetScopedAsync));
        var settlements = await ScopedSettlements(CurrentUser)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        LogKey(nameof(GetScopedAsync), "loaded scoped settlements", new { Count = settlements.Count });

        return settlements.Select(x => x.ToDto()).ToArray();
    }

    public async Task<SettlementDto> GetScopedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        LogReceived(nameof(GetScopedByIdAsync), new { Id = id });
        var settlement = await ScopedSettlements(CurrentUser).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Settlement was not found.");

        return settlement.ToDto();
    }

    private IQueryable<Settlement> ScopedSettlements(CurrentUser current)
    {
        var role = RoleNames.ParseRole(current.Role);
        var query = Db.Settlements.AsNoTracking();

        return role switch
        {
            UserRole.Hospital when current.OrganizationId.HasValue => query.Where(x => x.HospitalId == current.OrganizationId.Value),
            UserRole.InsuranceCompany when current.OrganizationId.HasValue => query.Where(x =>
                Db.FinancingRequests.Any(request =>
                    request.Id == x.FinancingRequestId &&
                    request.InsuranceCompanyId == current.OrganizationId.Value)),
            _ => throw new ForbiddenException("Settlements are not visible to this role.")
        };
    }
}
