using BallastLane.Application.Events;
using Microsoft.Extensions.Logging;

namespace BallastLane.Application.EventHandlers;

public sealed class TaskCreatedAuditHandler(ILogger<TaskCreatedAuditHandler> logger)
    : IDomainEventHandler<TaskCreatedEvent>
{
    public Task HandleAsync(TaskCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[AUDIT] {EventType} | UserId={UserId} | ResourceId={ResourceId} | At={OccurredAt}",
            nameof(TaskCreatedEvent),
            domainEvent.UserId,
            domainEvent.TaskId,
            domainEvent.OccurredAt);
        return Task.CompletedTask;
    }
}
