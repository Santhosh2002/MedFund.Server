using MedFund.Application.Common;
using MedFund.Application.Partnerships;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MedFund.Api.Controllers;

[ApiController]
[Route("api/partnerships")]
public sealed class PartnershipsController : ControllerBase
{
    private readonly IPartnershipService partnershipService;

    public PartnershipsController(IPartnershipService partnershipService)
    {
        this.partnershipService = partnershipService;
    }

    [AllowAnonymous]
    [EnableRateLimiting("partnership-submissions")]
    [HttpPost]
    [RequestSizeLimit(16 * 1024)]
    [ProducesResponseType(typeof(CreatePartnershipLeadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CreatePartnershipLeadResponse>> Create(
        CreatePartnershipLeadRequest request,
        CancellationToken cancellationToken)
    {
        var response = await partnershipService.CreateAsync(
            request,
            ReadClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Created($"/api/partnerships/{response.Id}", response);
    }

    private string? ReadClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
