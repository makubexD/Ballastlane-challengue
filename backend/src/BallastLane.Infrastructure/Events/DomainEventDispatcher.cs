using BallastLane.Application.Events;
using BallastLane.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallastLane.Infrastructure.Events;

public sealed class DomainEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handlers = serviceProvider.GetServices(handlerType).ToList();

        logger.LogDebug("Dispatching {EventType} to {HandlerCount} handler(s)", eventType.Name, handlers.Count);

        foreach (var handler in handlers)
        {
            try
            {
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Handler {HandlerType} failed for event {EventType}",
                    handler!.GetType().Name, eventType.Name);
            }
        }
    }
}
