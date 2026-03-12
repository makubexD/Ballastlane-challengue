namespace BallastLane.Domain.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
