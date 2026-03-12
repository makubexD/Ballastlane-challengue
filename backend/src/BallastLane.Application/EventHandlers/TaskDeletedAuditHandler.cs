using BallastLane.Application.Events;
using Microsoft.Extensions.Logging;

namespace BallastLane.Application.EventHandlers;

public sealed class TaskDeletedAuditHandler(ILogger<TaskDeletedAuditHandler> logger)
    : IDomainEventHandler<TaskDeletedEvent>
{
    public Task HandleAsync(TaskDeletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[AUDIT] {EventType} | UserId={UserId} | ResourceId={ResourceId} | At={OccurredAt}",
            nameof(TaskDeletedEvent),
            domainEvent.UserId,
            domainEvent.TaskId,
            domainEvent.OccurredAt);
        return Task.CompletedTask;
    }
}
