using FluentAssertions;
using MedFund.Application.Common;
using MedFund.Domain.Enums;
using Xunit;

namespace MedFund.Tests;

public sealed class RoleNamesTests
{
    [Theory]
    [InlineData(UserRole.Patient, "PATIENT")]
    [InlineData(UserRole.Hospital, "HOSPITAL")]
    [InlineData(UserRole.InsuranceCompany, "INSURANCE_COMPANY")]
    public void ToApiValue_returns_frontend_role_values(UserRole role, string expected)
    {
        RoleNames.ToApiValue(role).Should().Be(expected);
    }

    [Theory]
    [InlineData("AwaitingPatientConsent", "AWAITING_PATIENT_CONSENT")]
    [InlineData("InsuranceCompany", "INSURANCE_COMPANY")]
    public void ToUpperSnakeCase_matches_frontend_status_format(string input, string expected)
    {
        RoleNames.ToUpperSnakeCase(input).Should().Be(expected);
    }
}
