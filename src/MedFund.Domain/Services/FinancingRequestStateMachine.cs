using MedFund.Domain.Enums;

namespace MedFund.Domain.Services;

public static class FinancingRequestStateMachine
{
    private static readonly IReadOnlyDictionary<FinancingRequestStatus, FinancingRequestStatus[]> AllowedTransitions =
        new Dictionary<FinancingRequestStatus, FinancingRequestStatus[]>
        {
            [FinancingRequestStatus.Draft] =
            [
                FinancingRequestStatus.AwaitingPatientConsent,
                FinancingRequestStatus.Cancelled
            ],
            [FinancingRequestStatus.AwaitingPatientConsent] =
            [
                FinancingRequestStatus.ConsentReceived,
                FinancingRequestStatus.Cancelled
            ],
            [FinancingRequestStatus.ConsentReceived] =
            [
                FinancingRequestStatus.InsuranceReview,
                FinancingRequestStatus.Cancelled
            ],
            [FinancingRequestStatus.InsuranceReview] =
            [
                FinancingRequestStatus.Approved,
                FinancingRequestStatus.Rejected,
                FinancingRequestStatus.Cancelled
            ],
            [FinancingRequestStatus.Approved] =
            [
                FinancingRequestStatus.DisbursedToHospital,
                FinancingRequestStatus.Cancelled
            ],
            [FinancingRequestStatus.DisbursedToHospital] =
            [
                FinancingRequestStatus.Settled
            ],
            [FinancingRequestStatus.Rejected] = [],
            [FinancingRequestStatus.Settled] = [],
            [FinancingRequestStatus.Cancelled] = []
        };

    public static bool CanTransition(FinancingRequestStatus current, FinancingRequestStatus next)
    {
        return AllowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(next);
    }

    public static void EnsureCanTransition(FinancingRequestStatus current, FinancingRequestStatus next)
    {
        if (!CanTransition(current, next))
        {
            throw new InvalidOperationException($"Cannot move financing request from {current} to {next}.");
        }
    }
}
