using MedFund.Application.Common;
using MedFund.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFund.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService userService;

    public UsersController(IUserService userService)
    {
        this.userService = userService;
    }

    [HttpGet("me")]
    public Task<CurrentUserResponse> GetMe(CancellationToken cancellationToken)
    {
        return userService.GetCurrentAsync(cancellationToken);
    }

    [HttpPut("me")]
    public Task<CurrentUserResponse> UpdateMe(UpdateCurrentUserRequest request, CancellationToken cancellationToken)
    {
        return userService.UpdateCurrentAsync(request, cancellationToken);
    }
}
