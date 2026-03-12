using BallastLane.Domain.Events;

namespace BallastLane.Application.Events;

public sealed record UserRegisteredEvent(
    Guid UserId,
    string Email,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
