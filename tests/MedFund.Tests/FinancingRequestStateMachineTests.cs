using FluentAssertions;
using MedFund.Domain.Enums;
using MedFund.Domain.Services;
using Xunit;

namespace MedFund.Tests;

public sealed class FinancingRequestStateMachineTests
{
    [Fact]
    public void Draft_can_move_to_awaiting_patient_consent()
    {
        FinancingRequestStateMachine
            .CanTransition(FinancingRequestStatus.Draft, FinancingRequestStatus.AwaitingPatientConsent)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Draft_cannot_skip_to_approved()
    {
        FinancingRequestStateMachine
            .CanTransition(FinancingRequestStatus.Draft, FinancingRequestStatus.Approved)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Consent_received_can_move_to_insurance_review()
    {
        FinancingRequestStateMachine
            .CanTransition(FinancingRequestStatus.ConsentReceived, FinancingRequestStatus.InsuranceReview)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Settled_is_terminal()
    {
        FinancingRequestStateMachine
            .CanTransition(FinancingRequestStatus.Settled, FinancingRequestStatus.Cancelled)
            .Should()
            .BeFalse();
    }
}
