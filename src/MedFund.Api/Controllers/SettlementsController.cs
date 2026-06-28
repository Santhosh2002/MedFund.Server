using MedFund.Application.Common;
using MedFund.Application.Financing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFund.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/settlements")]
public sealed class SettlementsController : ControllerBase
{
    private readonly ISettlementService settlementService;

    public SettlementsController(ISettlementService settlementService)
    {
        this.settlementService = settlementService;
    }

    [HttpGet]
    public Task<IReadOnlyCollection<SettlementDto>> Get(CancellationToken cancellationToken)
    {
        return settlementService.GetScopedAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public Task<SettlementDto> GetById(Guid id, CancellationToken cancellationToken)
    {
        return settlementService.GetScopedByIdAsync(id, cancellationToken);
    }
}
