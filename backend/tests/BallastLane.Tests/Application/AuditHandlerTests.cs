using BallastLane.Application.EventHandlers;
using BallastLane.Application.Events;
using BallastLane.Tests.TestData;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BallastLane.Tests.Application;

public sealed class AuditHandlerTests
{
    [Fact]
    public async Task TaskCreatedAuditHandler_ShouldComplete_WhenHandled()
    {
        var handler = new TaskCreatedAuditHandler(NullLogger<TaskCreatedAuditHandler>.Instance);
        var domainEvent = new TaskCreatedEvent(
            TestConstants.ValidTaskId,
            TestConstants.ValidUserId,
            TestConstants.ValidTitle,
            TestConstants.FixedUtcNow);

        await handler.HandleAsync(domainEvent);
    }

    [Fact]
    public async Task TaskDeletedAuditHandler_ShouldComplete_WhenHandled()
    {
        var handler = new TaskDeletedAuditHandler(NullLogger<TaskDeletedAuditHandler>.Instance);
        var domainEvent = new TaskDeletedEvent(
            TestConstants.ValidTaskId,
            TestConstants.ValidUserId,
            TestConstants.FixedUtcNow);

        await handler.HandleAsync(domainEvent);
    }

    [Fact]
    public async Task UserRegisteredAuditHandler_ShouldComplete_WhenHandled()
    {
        var handler = new UserRegisteredAuditHandler(NullLogger<UserRegisteredAuditHandler>.Instance);
        var domainEvent = new UserRegisteredEvent(
            TestConstants.ValidUserId,
            TestConstants.ValidEmail,
            TestConstants.FixedUtcNow);

        await handler.HandleAsync(domainEvent);
    }

    [Fact]
    public async Task TaskCreatedAuditHandler_ShouldLog_WhenHandled()
    {
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<TaskCreatedAuditHandler>>();
        var handler = new TaskCreatedAuditHandler(logger.Object);
        var domainEvent = new TaskCreatedEvent(
            TestConstants.ValidTaskId,
            TestConstants.ValidUserId,
            TestConstants.ValidTitle,
            TestConstants.FixedUtcNow);

        await handler.HandleAsync(domainEvent);

        logger.Verify(
            l => l.Log(
                Microsoft.Extensions.Logging.LogLevel.Information,
                It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("AUDIT")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
    }

    [Fact]
    public async Task TaskDeletedAuditHandler_ShouldLog_WhenHandled()
    {
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<TaskDeletedAuditHandler>>();
        var handler = new TaskDeletedAuditHandler(logger.Object);
        var domainEvent = new TaskDeletedEvent(
            TestConstants.ValidTaskId,
            TestConstants.ValidUserId,
            TestConstants.FixedUtcNow);

        await handler.HandleAsync(domainEvent);

        logger.Verify(
            l => l.Log(
                Microsoft.Extensions.Logging.LogLevel.Information,
                It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("AUDIT")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
    }

    [Fact]
    public async Task UserRegisteredAuditHandler_ShouldLog_WhenHandled()
    {
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<UserRegisteredAuditHandler>>();
        var handler = new UserRegisteredAuditHandler(logger.Object);
        var domainEvent = new UserRegisteredEvent(
            TestConstants.ValidUserId,
            TestConstants.ValidEmail,
            TestConstants.FixedUtcNow);

        await handler.HandleAsync(domainEvent);

        logger.Verify(
            l => l.Log(
                Microsoft.Extensions.Logging.LogLevel.Information,
                It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("AUDIT")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());
    }
}
