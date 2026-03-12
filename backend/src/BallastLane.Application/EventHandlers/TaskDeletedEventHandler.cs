using BallastLane.Application.Events;
using Microsoft.Extensions.Logging;

namespace BallastLane.Application.EventHandlers;

public sealed class TaskDeletedEventHandler(ILogger<TaskDeletedEventHandler> logger)
    : IDomainEventHandler<TaskDeletedEvent>
{
    public Task HandleAsync(TaskDeletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Task deleted: {TaskId} by user {UserId} at {OccurredAt}",
            domainEvent.TaskId, domainEvent.UserId, domainEvent.OccurredAt);
        return Task.CompletedTask;
    }
}
