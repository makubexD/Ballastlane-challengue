namespace BallastLane.Application.DTOs;

public sealed record UserProfileResponse(Guid Id, string Email, DateTime CreatedAt);
