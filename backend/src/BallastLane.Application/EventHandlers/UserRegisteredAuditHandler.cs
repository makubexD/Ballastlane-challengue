using BallastLane.Application.Events;
using Microsoft.Extensions.Logging;

namespace BallastLane.Application.EventHandlers;

public sealed class UserRegisteredAuditHandler(ILogger<UserRegisteredAuditHandler> logger)
    : IDomainEventHandler<UserRegisteredEvent>
{
    public Task HandleAsync(UserRegisteredEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[AUDIT] {EventType} | UserId={UserId} | ResourceId={ResourceId} | At={OccurredAt}",
            nameof(UserRegisteredEvent),
            domainEvent.UserId,
            domainEvent.UserId,
            domainEvent.OccurredAt);
        return Task.CompletedTask;
    }
}
