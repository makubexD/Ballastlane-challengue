using BallastLane.Domain.Entities;

namespace BallastLane.Domain.Interfaces;

public interface IJwtProvider
{
    int ExpiryMinutes { get; }
    string Generate(User user);
}
