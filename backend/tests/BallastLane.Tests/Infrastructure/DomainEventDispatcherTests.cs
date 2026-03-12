using BallastLane.Application.Events;
using BallastLane.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BallastLane.Tests.Infrastructure;

public sealed class DomainEventDispatcherTests
{
    private static DomainEventDispatcher BuildSut(IServiceProvider serviceProvider) =>
        new(serviceProvider, NullLogger<DomainEventDispatcher>.Instance);

    [Fact]
    public async Task DispatchAsync_ShouldCallHandler_WhenHandlerIsRegistered()
    {
        var handler = new Mock<IDomainEventHandler<TaskCreatedEvent>>();
        handler
            .Setup(h => h.HandleAsync(It.IsAny<TaskCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddSingleton(handler.Object);
        var sut = BuildSut(services.BuildServiceProvider());
        var domainEvent = new TaskCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Test task", DateTime.UtcNow);

        await sut.DispatchAsync(domainEvent);

        handler.Verify(h => h.HandleAsync(domainEvent, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotThrow_WhenNoHandlersAreRegistered()
    {
        var services = new ServiceCollection();
        var sut = BuildSut(services.BuildServiceProvider());
        var domainEvent = new TaskCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Orphan event", DateTime.UtcNow);

        var exception = await Record.ExceptionAsync(() => sut.DispatchAsync(domainEvent));

        Assert.Null(exception);
    }

    [Fact]
    public async Task DispatchAsync_ShouldSwallowHandlerException_AndNotThrow()
    {
        var handler = new Mock<IDomainEventHandler<TaskCreatedEvent>>();
        handler
            .Setup(h => h.HandleAsync(It.IsAny<TaskCreatedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("handler exploded"));
        var services = new ServiceCollection();
        services.AddSingleton(handler.Object);
        var sut = BuildSut(services.BuildServiceProvider());
        var domainEvent = new TaskCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Failing task", DateTime.UtcNow);

        var exception = await Record.ExceptionAsync(() => sut.DispatchAsync(domainEvent));

        Assert.Null(exception);
    }
}
