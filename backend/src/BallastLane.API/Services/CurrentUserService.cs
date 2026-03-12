using System.Security.Claims;

namespace BallastLane.API.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private const string UserIdClaimType = "userId";

    public Guid UserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User?.FindFirst(UserIdClaimType)
                ?? httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID claim not found in token.");

            return Guid.TryParse(claim.Value, out var userId)
                ? userId
                : throw new UnauthorizedAccessException("User ID claim is not a valid GUID.");
        }
    }
}
