using MedFund.Application.Common;
using MedFund.Application.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedFund.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly IApplicationDbContext db;
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IAuditWriter auditWriter;
    private readonly ILogger<UserService> logger;

    public UserService(IApplicationDbContext db, ICurrentUserAccessor currentUserAccessor, IAuditWriter auditWriter, ILogger<UserService> logger)
    {
        this.db = db;
        this.currentUserAccessor = currentUserAccessor;
        this.auditWriter = auditWriter;
        this.logger = logger;
    }

    public async Task<CurrentUserResponse> GetCurrentAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("{Service}.{Function} received request.", nameof(UserService), nameof(GetCurrentAsync));
        var current = currentUserAccessor.GetRequiredCurrentUser();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == current.UserId && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Current user was not found.");
        logger.LogInformation("{Service}.{Function} loaded current user. UserId={UserId}, Role={Role}", nameof(UserService), nameof(GetCurrentAsync), user.Id, user.Role);

        return user.ToCurrentUserResponse();
    }

    public async Task<CurrentUserResponse> UpdateCurrentAsync(UpdateCurrentUserRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("{Service}.{Function} received request. FirstName={FirstName}, LastName={LastName}, PhoneNumber={PhoneNumber}, HasProfilePicture={HasProfilePicture}", nameof(UserService), nameof(UpdateCurrentAsync), request.FirstName, request.LastName, request.PhoneNumber, !string.IsNullOrWhiteSpace(request.ProfilePictureUrl));
        var current = currentUserAccessor.GetRequiredCurrentUser();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == current.UserId && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Current user was not found.");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.ProfilePictureUrl = request.ProfilePictureUrl;
        auditWriter.Add(user.Id, "UserProfileUpdated", "User", user.Id.ToString());
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Service}.{Function} updated user profile. UserId={UserId}", nameof(UserService), nameof(UpdateCurrentAsync), user.Id);

        return user.ToCurrentUserResponse();
    }
}
