using MedFund.Application.Auth;
using MedFund.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFund.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService authService;
    private readonly IUserService userService;

    public AuthController(IAuthService authService, IUserService userService)
    {
        this.authService = authService;
        this.userService = userService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public Task<AuthResponse> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return authService.LoginAsync(request, cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("signup")]
    public Task<AuthResponse> Signup(SignupRequest request, CancellationToken cancellationToken)
    {
        return authService.SignupAsync(request, cancellationToken);
    }

    [Authorize]
    [HttpGet("me")]
    public Task<MedFund.Application.Users.CurrentUserResponse> Me(CancellationToken cancellationToken)
    {
        return userService.GetCurrentAsync(cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public Task<TokenResponse> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var token = ReadBearerToken() ?? request.RefreshToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ForbiddenException("Refresh token is required.");
        }

        return authService.RefreshAsync(token, cancellationToken);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        var token = ReadBearerToken() ?? request.RefreshToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            await authService.LogoutAsync(token, cancellationToken);
        }

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.AcceptForgotPasswordAsync(request, cancellationToken);
        return Accepted();
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken cancellationToken)
    {
        await authService.VerifyEmailAsync(token, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        await authService.ResendVerificationAsync(request, cancellationToken);
        return Accepted();
    }

    private string? ReadBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..].Trim() : null;
    }
}
