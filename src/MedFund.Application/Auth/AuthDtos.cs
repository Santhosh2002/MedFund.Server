namespace MedFund.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record SignupRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? PhoneNumber);

public sealed record RefreshRequest(string? RefreshToken);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string? Token, string? CurrentPassword, string NewPassword);

public sealed record ResendVerificationRequest(string Email);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string Email,
    string Role);

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);
