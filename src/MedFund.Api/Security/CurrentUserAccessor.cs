using System.IdentityModel.Tokens.Jwt;
using MedFund.Application.Common;

namespace MedFund.Api.Security;

public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser GetRequiredCurrentUser()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new ForbiddenException("Authentication is required.");
        }

        var userId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var email = user.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        var role = user.FindFirst("role")?.Value;
        if (!Guid.TryParse(userId, out var parsedUserId) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(role))
        {
            throw new ForbiddenException("Authenticated user context is incomplete.");
        }

        return new CurrentUser(
            parsedUserId,
            email,
            role,
            TryGetGuidClaim("organizationId"),
            TryGetGuidClaim("patientId"));

        Guid? TryGetGuidClaim(string name)
        {
            var value = user.FindFirst(name)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
