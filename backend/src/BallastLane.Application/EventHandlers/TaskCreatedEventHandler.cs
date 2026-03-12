using BallastLane.Application.Events;
using Microsoft.Extensions.Logging;

namespace BallastLane.Application.EventHandlers;

public sealed class TaskCreatedEventHandler(ILogger<TaskCreatedEventHandler> logger)
    : IDomainEventHandler<TaskCreatedEvent>
{
    public Task HandleAsync(TaskCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Task created: {TaskId} (title: {Title}) by user {UserId} at {OccurredAt}",
            domainEvent.TaskId, domainEvent.Title, domainEvent.UserId, domainEvent.OccurredAt);
        return Task.CompletedTask;
    }
}
