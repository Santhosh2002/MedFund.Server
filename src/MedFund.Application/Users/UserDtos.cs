namespace MedFund.Application.Users;

public sealed record CurrentUserResponse(
    string Id,
    string FullName,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? ProfilePictureUrl,
    string Role,
    string? OrganizationId,
    string? PatientId,
    bool EmailVerified);

public sealed record UpdateCurrentUserRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? ProfilePictureUrl);
