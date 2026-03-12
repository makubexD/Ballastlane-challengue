using BallastLane.Application.Events;
using Microsoft.Extensions.Logging;

namespace BallastLane.Application.EventHandlers;

public sealed class UserRegisteredEventHandler(ILogger<UserRegisteredEventHandler> logger)
    : IDomainEventHandler<UserRegisteredEvent>
{
    public Task HandleAsync(UserRegisteredEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "User registered: {UserId} (email: {Email}) at {OccurredAt}",
            domainEvent.UserId, domainEvent.Email, domainEvent.OccurredAt);
        return Task.CompletedTask;
    }
}
