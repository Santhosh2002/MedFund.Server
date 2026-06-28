namespace MedFund.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "MedFund";

    public string Audience { get; init; } = "MedFund.Frontend";

    public string SigningKey { get; init; } = "replace-with-a-development-secret-of-at-least-32-characters";

    public int AccessTokenMinutes { get; init; } = 60;

    public int RefreshTokenDays { get; init; } = 14;
}
