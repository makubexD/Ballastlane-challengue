using System.Security.Claims;
using BallastLane.Application.Common;

namespace BallastLane.API.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimNames.UserId)
                ?? httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID claim not found in token.");

            return Guid.TryParse(claim.Value, out var userId)
                ? userId
                : throw new UnauthorizedAccessException("User ID claim is not a valid GUID.");
        }
    }
}
