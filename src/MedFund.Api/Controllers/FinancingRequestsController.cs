using MedFund.Application.Common;
using MedFund.Application.Financing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFund.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/financing-requests")]
public sealed class FinancingRequestsController : ControllerBase
{
    private readonly IFinancingRequestService financingRequestService;

    public FinancingRequestsController(IFinancingRequestService financingRequestService)
    {
        this.financingRequestService = financingRequestService;
    }

    [HttpGet]
    public Task<PagedResult<FinancingRequestDto>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return financingRequestService.GetAllAsync(page, pageSize, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public Task<FinancingRequestDto> GetById(Guid id, CancellationToken cancellationToken)
    {
        return financingRequestService.GetByIdAsync(id, cancellationToken);
    }
}
