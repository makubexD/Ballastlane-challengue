using BallastLane.Domain.Entities;

namespace BallastLane.Domain.Interfaces;

public interface IJwtProvider
{
    string Generate(User user);
}
