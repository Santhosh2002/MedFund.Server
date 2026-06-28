using MedFund.Api.Security;
using MedFund.Application.Common;
using MedFund.Application.Financing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFund.Api.Controllers;

[ApiController]
[Authorize(Policy = PolicyNames.Patient)]
[Route("api/patient")]
public sealed class PatientController : ControllerBase
{
    private readonly IPatientService patientService;

    public PatientController(IPatientService patientService)
    {
        this.patientService = patientService;
    }

    [HttpGet("me")]
    public Task<PatientDto> GetMe(CancellationToken cancellationToken)
    {
        return patientService.GetMeAsync(cancellationToken);
    }

    [HttpPut("me")]
    public Task<PatientDto> UpdateMe(UpdatePatientProfileRequest request, CancellationToken cancellationToken)
    {
        return patientService.UpdateMeAsync(request, cancellationToken);
    }

    [HttpGet("dashboard")]
    public Task<PatientDashboardResponse> Dashboard(CancellationToken cancellationToken)
    {
        return patientService.GetDashboardAsync(cancellationToken);
    }

    [HttpGet("financing-requests")]
    public Task<IReadOnlyCollection<FinancingRequestDto>> FinancingRequests(CancellationToken cancellationToken)
    {
        return patientService.GetFinancingRequestsAsync(cancellationToken);
    }

    [HttpGet("financing-requests/{id:guid}")]
    public Task<FinancingRequestDto> FinancingRequest(Guid id, CancellationToken cancellationToken)
    {
        return patientService.GetFinancingRequestAsync(id, cancellationToken);
    }

    [HttpPost("financing-requests/{id:guid}/consent")]
    public Task<ConsentResponse> Consent(Guid id, ConsentRequest request, CancellationToken cancellationToken)
    {
        return patientService.ConsentAsync(
            id,
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
    }

    [HttpGet("repayments")]
    public Task<IReadOnlyCollection<EmiScheduleItemDto>> Repayments(CancellationToken cancellationToken)
    {
        return patientService.GetRepaymentsAsync(cancellationToken);
    }
}
