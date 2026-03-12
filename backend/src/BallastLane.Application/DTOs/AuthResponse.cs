namespace BallastLane.Application.DTOs;

public sealed record AuthResponse(string Token, DateTime ExpiresAt, Guid UserId);
