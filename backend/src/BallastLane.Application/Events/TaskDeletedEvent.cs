using BallastLane.Domain.Events;

namespace BallastLane.Application.Events;

public sealed record TaskDeletedEvent(
    Guid TaskId,
    Guid UserId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
