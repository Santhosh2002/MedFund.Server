using FluentAssertions;
using MedFund.Application.Financing;
using MedFund.Application.Partnerships;
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

    [Fact]
    public void Partnership_lead_accepts_healthcare_provider_submission()
    {
        var validator = new CreatePartnershipLeadRequestValidator();
        var request = new CreatePartnershipLeadRequest(
            "Priya",
            "Nair",
            "+91 98765 43210",
            "priya@citycare.example",
            "HEALTHCARE_PROVIDER",
            "CityCare Multispeciality Hospital");

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Partnership_lead_accepts_nbfc_submission()
    {
        var validator = new CreatePartnershipLeadRequestValidator();
        var request = new CreatePartnershipLeadRequest(
            "Rahul",
            "Mehta",
            "+91 99887 76655",
            "rahul@financepartner.example",
            "NBFC",
            "Aegis Finance");

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Partnership_lead_rejects_invalid_email()
    {
        var validator = new CreatePartnershipLeadRequestValidator();
        var request = ValidPartnershipLeadRequest() with { Email = "not-an-email" };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreatePartnershipLeadRequest.Email));
    }

    [Fact]
    public void Partnership_lead_requires_organization_name()
    {
        var validator = new CreatePartnershipLeadRequestValidator();
        var request = ValidPartnershipLeadRequest() with { OrganizationName = "" };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreatePartnershipLeadRequest.OrganizationName));
    }

    [Fact]
    public void Partnership_lead_rejects_unknown_partner_type()
    {
        var validator = new CreatePartnershipLeadRequestValidator();
        var request = ValidPartnershipLeadRequest() with { PartnerType = "BANK" };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreatePartnershipLeadRequest.PartnerType));
    }

    private static CreatePartnershipLeadRequest ValidPartnershipLeadRequest()
    {
        return new CreatePartnershipLeadRequest(
            "Priya",
            "Nair",
            "+91 98765 43210",
            "priya@citycare.example",
            "HEALTHCARE_PROVIDER",
            "CityCare Multispeciality Hospital");
    }
}
