using BallastLane.Application.Services;
using BallastLane.Application.Validators;
using BallastLane.Domain.Events;
using BallastLane.Domain.Interfaces;
using BallastLane.Tests.TestData;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BallastLane.Tests.Application;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task CreateTask_ShouldCommit_WhenTaskIsValid()
    {
        // Arrange
        var taskRepo = new Mock<ITaskRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var sut = new TaskService(taskRepo.Object, unitOfWork.Object, new TaskValidator(), clock.Object, NullLogger<TaskService>.Instance, dispatcher.Object);
        var request = TestDataBuilder.ValidCreateRequest();

        // Act
        var result = await sut.CreateTaskAsync(request, TestConstants.ValidUserId);

        // Assert
        Assert.True(result.IsSuccess);
        unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CreateTask_ShouldNotCommit_WhenValidationFails()
    {
        // Arrange
        var taskRepo = new Mock<ITaskRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var sut = new TaskService(taskRepo.Object, unitOfWork.Object, new TaskValidator(), clock.Object, NullLogger<TaskService>.Instance, dispatcher.Object);
        var invalidRequest = TestDataBuilder.ValidCreateRequest(title: string.Empty);

        // Act
        var result = await sut.CreateTaskAsync(invalidRequest, TestConstants.ValidUserId);

        // Assert
        Assert.True(result.IsFailure);
        unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task DeleteTask_ShouldCommit_WhenTaskExists()
    {
        // Arrange
        var taskRepo = new Mock<ITaskRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clock = new Mock<IDateTimeProvider>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var existingTask = TestDataBuilder.ValidTask(
            id: TestConstants.ValidTaskId,
            userId: TestConstants.ValidUserId);
        taskRepo
            .Setup(r => r.GetByIdAsync(TestConstants.ValidTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);
        var sut = new TaskService(taskRepo.Object, unitOfWork.Object, new TaskValidator(), clock.Object, NullLogger<TaskService>.Instance, dispatcher.Object);

        // Act
        var result = await sut.DeleteTaskAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId);

        // Assert
        Assert.True(result.IsSuccess);
        unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once());
    }
}
