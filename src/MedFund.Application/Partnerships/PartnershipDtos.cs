namespace MedFund.Application.Partnerships;

public sealed record CreatePartnershipLeadRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Email,
    string PartnerType,
    string OrganizationName);

public sealed record CreatePartnershipLeadResponse(
    Guid Id,
    string Message);
