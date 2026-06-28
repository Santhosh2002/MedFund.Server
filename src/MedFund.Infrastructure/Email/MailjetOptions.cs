namespace MedFund.Infrastructure.Email;

public sealed class MailjetOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; init; }

    public string ApiKey { get; init; } = string.Empty;

    public string ApiSecret { get; init; } = string.Empty;

    public string FromEmail { get; init; } = "no-reply@medfund.local";

    public string FromName { get; init; } = "MedFund";

    public string FrontendBaseUrl { get; init; } = "http://localhost:3000";

    public string VerifyEmailPath { get; init; } = "/verify-email";

    public string ResetPasswordPath { get; init; } = "/reset-password";
}
