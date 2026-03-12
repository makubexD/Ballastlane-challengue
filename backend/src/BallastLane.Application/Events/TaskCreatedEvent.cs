using BallastLane.Domain.Events;

namespace BallastLane.Application.Events;

public sealed record TaskCreatedEvent(
    Guid TaskId,
    Guid UserId,
    string Title,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
