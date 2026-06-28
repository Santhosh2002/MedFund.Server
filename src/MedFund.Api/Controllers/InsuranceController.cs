using MedFund.Api.Security;
using MedFund.Application.Common;
using MedFund.Application.Financing;
using MedFund.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFund.Api.Controllers;

[ApiController]
[Authorize(Policy = PolicyNames.InsuranceCompany)]
[Route("api/insurance")]
public sealed class InsuranceController : ControllerBase
{
    private readonly IInsuranceService insuranceService;

    public InsuranceController(IInsuranceService insuranceService)
    {
        this.insuranceService = insuranceService;
    }

    [HttpGet("dashboard")]
    public Task<InsuranceDashboardResponse> Dashboard(CancellationToken cancellationToken)
    {
        return insuranceService.GetDashboardAsync(cancellationToken);
    }

    [HttpGet("cases")]
    public Task<PagedResult<FinancingRequestDto>> Cases(
        [FromQuery] string? reviewStatus,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return insuranceService.GetCasesAsync(
            ParseEnum<InsuranceReviewStatus>(reviewStatus),
            ParseEnum<FinancingRequestStatus>(status),
            page,
            pageSize,
            cancellationToken);
    }

    [HttpGet("cases/{id:guid}")]
    public Task<FinancingRequestDto> Case(Guid id, CancellationToken cancellationToken)
    {
        return insuranceService.GetCaseAsync(id, cancellationToken);
    }

    [HttpPost("cases/{id:guid}/decision")]
    public Task<InsuranceDecisionResponse> Decision(Guid id, InsuranceDecisionRequest request, CancellationToken cancellationToken)
    {
        return insuranceService.DecideAsync(id, request, cancellationToken);
    }

    [HttpGet("coverage-review")]
    public Task<PagedResult<FinancingRequestDto>> CoverageReview([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return insuranceService.GetCasesAsync(InsuranceReviewStatus.Pending, FinancingRequestStatus.InsuranceReview, page, pageSize, cancellationToken);
    }

    [HttpGet("settlements")]
    public Task<IReadOnlyCollection<SettlementDto>> Settlements(CancellationToken cancellationToken)
    {
        return insuranceService.GetSettlementsAsync(cancellationToken);
    }

    private static TEnum? ParseEnum<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        return Enum.TryParse<TEnum>(normalized, ignoreCase: true, out var parsed)
            ? parsed
            : throw new MedFundException($"{value} is not a valid {typeof(TEnum).Name}.");
    }
}
