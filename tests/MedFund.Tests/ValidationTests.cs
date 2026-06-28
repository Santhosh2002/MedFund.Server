using FluentAssertions;
using MedFund.Application.Financing;
using MedFund.Application.Validation;
using MedFund.Domain.Enums;
using Xunit;

namespace MedFund.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void Financing_request_rejects_requested_amount_above_gap()
    {
        var validator = new CreateFinancingRequestRequestValidator();
        var request = new CreateFinancingRequestRequest(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Cardiac procedure",
            200000m,
            140000m,
            70000m);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateFinancingRequestRequest.RequestedFinanceAmount));
    }

    [Fact]
    public void Consent_requires_acceptance()
    {
        var validator = new ConsentRequestValidator();
        var request = new ConsentRequest(false, "Financing review", "2026-06-01");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(ConsentRequest.Accepted));
    }

    [Fact]
    public void Insurance_rejection_requires_notes()
    {
        var validator = new InsuranceDecisionRequestValidator();
        var request = new InsuranceDecisionRequest(InsuranceReviewStatus.Rejected, 0m, null);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(InsuranceDecisionRequest.Notes));
    }

    [Fact]
    public void Insurance_approval_allows_empty_notes()
    {
        var validator = new InsuranceDecisionRequestValidator();
        var request = new InsuranceDecisionRequest(InsuranceReviewStatus.Approved, 140000m, null);

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
