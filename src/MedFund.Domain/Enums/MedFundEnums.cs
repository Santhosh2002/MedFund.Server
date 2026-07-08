namespace MedFund.Domain.Enums;

public enum UserRole
{
    Patient,
    Hospital,
    InsuranceCompany
}

public enum OrganizationType
{
    Hospital,
    InsuranceCompany,
    MedFund
}

public enum FinancingRequestStatus
{
    Draft,
    AwaitingPatientConsent,
    ConsentReceived,
    InsuranceReview,
    Approved,
    Rejected,
    DisbursedToHospital,
    Settled,
    Cancelled
}

public enum InsuranceReviewStatus
{
    Pending,
    Approved,
    Rejected,
    NeedsInfo
}

public enum SettlementStatus
{
    Pending,
    InProgress,
    Settled,
    Exception
}

public enum KycStatus
{
    Pending,
    Verified,
    Rejected
}

public enum ConsentStatus
{
    Pending,
    Accepted,
    Revoked
}

public enum EmiScheduleStatus
{
    Pending,
    Paid,
    Overdue,
    Waived
}

public enum InsurancePolicyStatus
{
    Active,
    Expired,
    Cancelled
}

public enum DocumentType
{
    AdmissionNote,
    Estimate,
    InsuranceApproval,
    DischargeSummary,
    Invoice,
    Other
}

public enum PartnershipPartnerType
{
    HealthcareProvider,
    Nbfc
}

public enum PartnershipLeadStatus
{
    New,
    Contacted,
    Qualified,
    Rejected,
    Converted
}
