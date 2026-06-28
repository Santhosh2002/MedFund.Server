using MedFund.Application.Auth;
using MedFund.Application.Common;
using MedFund.Domain.Entities;
using MedFund.Domain.Enums;
using MedFund.Infrastructure.Auth;
using MedFund.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedFund.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private const string PasswordResetPurpose = "PasswordReset";
    private const string EmailVerificationPurpose = "EmailVerification";

    private readonly IApplicationDbContext db;
    private readonly IPasswordHasher passwordHasher;
    private readonly IJwtTokenService jwtTokenService;
    private readonly IAuditWriter auditWriter;
    private readonly IEmailSender emailSender;
    private readonly JwtOptions jwtOptions;
    private readonly MailjetOptions emailOptions;
    private readonly ILogger<AuthService> logger;

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IAuditWriter auditWriter,
        IEmailSender emailSender,
        IOptions<JwtOptions> jwtOptions,
        IOptions<MailjetOptions> emailOptions,
        ILogger<AuthService> logger)
    {
        this.db = db;
        this.passwordHasher = passwordHasher;
        this.jwtTokenService = jwtTokenService;
        this.auditWriter = auditWriter;
        this.emailSender = emailSender;
        this.jwtOptions = jwtOptions.Value;
        this.emailOptions = emailOptions.Value;
        this.logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("{Service}.{Function} received login request. Email={Email}", nameof(AuthService), nameof(LoginAsync), request.Email);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == request.Email && x.IsActive, cancellationToken);
        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            logger.LogWarning("{Service}.{Function} login failed. Email={Email}", nameof(AuthService), nameof(LoginAsync), request.Email);
            throw new ForbiddenException("Invalid email or password.");
        }

        var refreshToken = AddRefreshToken(user);
        auditWriter.Add(user.Id, "UserLogin", nameof(User), user.Id.ToString());
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Service}.{Function} login succeeded. UserId={UserId}, Role={Role}", nameof(AuthService), nameof(LoginAsync), user.Id, RoleNames.ToApiValue(user.Role));

        return new AuthResponse(
            jwtTokenService.CreateAccessToken(user),
            refreshToken,
            jwtOptions.AccessTokenMinutes * 60,
            user.Email,
            RoleNames.ToApiValue(user.Role));
    }

    public async Task<AuthResponse> SignupAsync(SignupRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("{Service}.{Function} received signup request. Email={Email}, FirstName={FirstName}, LastName={LastName}", nameof(AuthService), nameof(SignupAsync), request.Email, request.FirstName, request.LastName);
        if (await db.Users.AnyAsync(x => x.Email == request.Email, cancellationToken))
        {
            logger.LogWarning("{Service}.{Function} rejected duplicate signup. Email={Email}", nameof(AuthService), nameof(SignupAsync), request.Email);
            throw new ConflictException("A user with this email already exists.");
        }

        var patient = new Patient
        {
            FullName = $"{request.FirstName} {request.LastName}",
            Mobile = request.PhoneNumber ?? string.Empty,
            Email = request.Email
        };
        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Role = UserRole.Patient,
            Patient = patient,
            PatientId = patient.Id
        };
        patient.User = user;
        patient.UserId = user.Id;

        db.Patients.Add(patient);
        db.Users.Add(user);
        var refreshToken = AddRefreshToken(user);
        var verificationToken = AddSecurityToken(user, EmailVerificationPurpose, TimeSpan.FromDays(2));
        auditWriter.Add(user.Id, "UserSignup", nameof(User), user.Id.ToString());
        await db.SaveChangesAsync(cancellationToken);
        await SendVerificationEmailAsync(user, verificationToken, cancellationToken);
        logger.LogInformation("{Service}.{Function} signup completed. UserId={UserId}, PatientId={PatientId}", nameof(AuthService), nameof(SignupAsync), user.Id, patient.Id);

        return new AuthResponse(
            jwtTokenService.CreateAccessToken(user),
            refreshToken,
            jwtOptions.AccessTokenMinutes * 60,
            user.Email,
            RoleNames.ToApiValue(user.Role));
    }

    public async Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        logger.LogInformation("{Service}.{Function} received refresh request.", nameof(AuthService), nameof(RefreshAsync));
        var hash = jwtTokenService.HashToken(refreshToken);
        var token = await db.RefreshTokens.Include(x => x.User).FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (token?.User is null || !token.IsActive || !token.User.IsActive)
        {
            logger.LogWarning("{Service}.{Function} refresh rejected because token was invalid or expired.", nameof(AuthService), nameof(RefreshAsync));
            throw new ForbiddenException("Refresh token is invalid or expired.");
        }

        token.RevokedAt = DateTimeOffset.UtcNow;
        var newRefreshToken = AddRefreshToken(token.User);
        token.ReplacedByTokenHash = jwtTokenService.HashToken(newRefreshToken);
        auditWriter.Add(token.UserId, "RefreshTokenRotated", nameof(RefreshToken), token.Id.ToString());
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Service}.{Function} refresh token rotated. UserId={UserId}, RefreshTokenId={RefreshTokenId}", nameof(AuthService), nameof(RefreshAsync), token.UserId, token.Id);

        return new TokenResponse(
            jwtTokenService.CreateAccessToken(token.User),
            newRefreshToken,
            jwtOptions.AccessTokenMinutes * 60);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        logger.LogInformation("{Service}.{Function} received logout request.", nameof(AuthService), nameof(LogoutAsync));
        var hash = jwtTokenService.HashToken(refreshToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (token is null)
        {
            logger.LogInformation("{Service}.{Function} logout ignored because refresh token was not found.", nameof(AuthService), nameof(LogoutAsync));
            return;
        }

        token.RevokedAt = DateTimeOffset.UtcNow;
        auditWriter.Add(token.UserId, "UserLogout", nameof(RefreshToken), token.Id.ToString());
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Service}.{Function} logout completed. UserId={UserId}, RefreshTokenId={RefreshTokenId}", nameof(AuthService), nameof(LogoutAsync), token.UserId, token.Id);
    }

    public async Task AcceptForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("{Service}.{Function} received forgot password request. Email={Email}", nameof(AuthService), nameof(AcceptForgotPasswordAsync), request.Email);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == request.Email && x.IsActive, cancellationToken);
        if (user is null)
        {
            logger.LogInformation("{Service}.{Function} ignored forgot password request for unknown or inactive email. Email={Email}", nameof(AuthService), nameof(AcceptForgotPasswordAsync), request.Email);
            return;
        }

        var resetToken = AddSecurityToken(user, PasswordResetPurpose, TimeSpan.FromHours(1));
        auditWriter.Add(user.Id, "PasswordResetRequested", nameof(User), user.Id.ToString());
        await db.SaveChangesAsync(cancellationToken);
        await SendPasswordResetEmailAsync(user, resetToken, cancellationToken);
        logger.LogInformation("{Service}.{Function} password reset email requested. UserId={UserId}", nameof(AuthService), nameof(AcceptForgotPasswordAsync), user.Id);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("{Service}.{Function} received reset password request. HasToken={HasToken}", nameof(AuthService), nameof(ResetPasswordAsync), !string.IsNullOrWhiteSpace(request.Token));
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new MedFundException("Password reset token is required.");
        }

        var token = await LoadSecurityTokenAsync(request.Token, PasswordResetPurpose, cancellationToken);
        if (token.User is null)
        {
            throw new ForbiddenException("Password reset token is invalid or expired.");
        }

        token.User.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
        token.ConsumedAt = DateTimeOffset.UtcNow;
        auditWriter.Add(token.UserId, "PasswordResetCompleted", nameof(User), token.UserId.ToString());
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Service}.{Function} password reset completed. UserId={UserId}", nameof(AuthService), nameof(ResetPasswordAsync), token.UserId);
    }

    public async Task VerifyEmailAsync(string token, CancellationToken cancellationToken)
    {
        logger.LogInformation("{Service}.{Function} received verify email request. HasToken={HasToken}", nameof(AuthService), nameof(VerifyEmailAsync), !string.IsNullOrWhiteSpace(token));
        var securityToken = await LoadSecurityTokenAsync(token, EmailVerificationPurpose, cancellationToken);
        if (securityToken.User is null)
        {
            throw new ForbiddenException("Email verification token is invalid or expired.");
        }

        securityToken.User.EmailVerifiedAt ??= DateTimeOffset.UtcNow;
        securityToken.ConsumedAt = DateTimeOffset.UtcNow;
        auditWriter.Add(securityToken.UserId, "EmailVerified", nameof(User), securityToken.UserId.ToString());
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Service}.{Function} email verified. UserId={UserId}", nameof(AuthService), nameof(VerifyEmailAsync), securityToken.UserId);
    }

    public async Task ResendVerificationAsync(ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("{Service}.{Function} received resend verification request. Email={Email}", nameof(AuthService), nameof(ResendVerificationAsync), request.Email);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == request.Email && x.IsActive, cancellationToken);
        if (user is null || user.EmailVerifiedAt.HasValue)
        {
            logger.LogInformation("{Service}.{Function} skipped verification resend. Email={Email}, Found={Found}, AlreadyVerified={AlreadyVerified}", nameof(AuthService), nameof(ResendVerificationAsync), request.Email, user is not null, user?.EmailVerifiedAt.HasValue == true);
            return;
        }

        var verificationToken = AddSecurityToken(user, EmailVerificationPurpose, TimeSpan.FromDays(2));
        auditWriter.Add(user.Id, "EmailVerificationRequested", nameof(User), user.Id.ToString());
        await db.SaveChangesAsync(cancellationToken);
        await SendVerificationEmailAsync(user, verificationToken, cancellationToken);
        logger.LogInformation("{Service}.{Function} verification email resent. UserId={UserId}", nameof(AuthService), nameof(ResendVerificationAsync), user.Id);
    }

    private string AddRefreshToken(User user)
    {
        var refreshToken = jwtTokenService.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            User = user,
            UserId = user.Id,
            TokenHash = jwtTokenService.HashToken(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtOptions.RefreshTokenDays)
        });

        return refreshToken;
    }

    private string AddSecurityToken(User user, string purpose, TimeSpan lifetime)
    {
        var token = jwtTokenService.CreateRefreshToken();
        db.UserSecurityTokens.Add(new UserSecurityToken
        {
            User = user,
            UserId = user.Id,
            Purpose = purpose,
            TokenHash = jwtTokenService.HashToken(token),
            ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime)
        });

        return token;
    }

    private Task SendVerificationEmailAsync(User user, string token, CancellationToken cancellationToken)
    {
        var link = BuildFrontendLink(emailOptions.VerifyEmailPath, token);
        var message = new EmailMessage(
            FromAddress(),
            [new EmailAddress(user.Email, $"{user.FirstName} {user.LastName}")],
            "Verify your MedFund email",
            $"Welcome to MedFund. Verify your email using this link: {link}",
            $"""
            <p>Welcome to MedFund, {System.Net.WebUtility.HtmlEncode(user.FirstName)}.</p>
            <p>Please verify your email address to finish setting up your account.</p>
            <p><a href="{System.Net.WebUtility.HtmlEncode(link)}">Verify email</a></p>
            """);

        return emailSender.SendAsync(message, cancellationToken);
    }

    private Task SendPasswordResetEmailAsync(User user, string token, CancellationToken cancellationToken)
    {
        var link = BuildFrontendLink(emailOptions.ResetPasswordPath, token);
        var message = new EmailMessage(
            FromAddress(),
            [new EmailAddress(user.Email, $"{user.FirstName} {user.LastName}")],
            "Reset your MedFund password",
            $"Reset your MedFund password using this link: {link}",
            $"""
            <p>Hello {System.Net.WebUtility.HtmlEncode(user.FirstName)},</p>
            <p>Use the link below to reset your MedFund password. This link expires in 1 hour.</p>
            <p><a href="{System.Net.WebUtility.HtmlEncode(link)}">Reset password</a></p>
            """);

        return emailSender.SendAsync(message, cancellationToken);
    }

    private EmailAddress FromAddress()
    {
        return new EmailAddress(emailOptions.FromEmail, emailOptions.FromName);
    }

    private string BuildFrontendLink(string path, string token)
    {
        var baseUri = new Uri(emailOptions.FrontendBaseUrl.TrimEnd('/') + "/");
        var relativePath = path.TrimStart('/');
        var builder = new UriBuilder(new Uri(baseUri, relativePath))
        {
            Query = $"token={Uri.EscapeDataString(token)}"
        };

        return builder.Uri.ToString();
    }

    private async Task<UserSecurityToken> LoadSecurityTokenAsync(string token, string purpose, CancellationToken cancellationToken)
    {
        var hashes = NormalizeSecurityTokenCandidates(token)
            .Select(jwtTokenService.HashToken)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var securityToken = await db.UserSecurityTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => hashes.Contains(x.TokenHash) && x.Purpose == purpose, cancellationToken);

        if (securityToken is null || !securityToken.IsActive || securityToken.User?.IsActive != true)
        {
            throw new ForbiddenException($"{purpose} token is invalid or expired.");
        }

        return securityToken;
    }

    private static IEnumerable<string> NormalizeSecurityTokenCandidates(string token)
    {
        var trimmed = token.Trim();
        if (trimmed.Length == 0)
        {
            yield break;
        }

        yield return trimmed;

        var plusRestored = trimmed.Replace(' ', '+');
        if (!string.Equals(plusRestored, trimmed, StringComparison.Ordinal))
        {
            yield return plusRestored;
        }

        var decoded = Uri.UnescapeDataString(trimmed);
        if (!string.Equals(decoded, trimmed, StringComparison.Ordinal))
        {
            yield return decoded;

            var decodedPlusRestored = decoded.Replace(' ', '+');
            if (!string.Equals(decodedPlusRestored, decoded, StringComparison.Ordinal))
            {
                yield return decodedPlusRestored;
            }
        }
    }
}
