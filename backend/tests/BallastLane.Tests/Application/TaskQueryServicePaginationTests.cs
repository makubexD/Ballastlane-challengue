using BallastLane.Application.Common;
using BallastLane.Application.Services;
using BallastLane.Application.Validators;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Events;
using BallastLane.Domain.Interfaces;
using BallastLane.Tests.TestData;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BallastLane.Tests.Application;

public sealed class TaskQueryServicePaginationTests
{
    private static TaskService BuildService(Mock<ITaskRepository> repo)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var eventDispatcher = new Mock<IDomainEventDispatcher>();
        return new TaskService(
            repo.Object,
            unitOfWork.Object,
            new TaskValidator(),
            clock.Object,
            NullLogger<TaskService>.Instance,
            eventDispatcher.Object);
    }

    [Fact]
    public async Task GetAllTasksAsync_ShouldReturnPagedResult_WhenParamsAreValid()
    {
        var repo = new Mock<ITaskRepository>();
        IReadOnlyList<TaskItem> items = [TestDataBuilder.ValidTask()];
        repo.Setup(r => r.GetPagedByUserIdAsync(TestConstants.ValidUserId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));
        var sut = BuildService(repo);

        var result = await sut.GetAllTasksAsync(TestConstants.ValidUserId, 1, 20, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.PageSize);
        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task GetAllTasksAsync_ShouldCallRepositoryWithCorrectPageParams()
    {
        var repo = new Mock<ITaskRepository>();
        repo.Setup(r => r.GetPagedByUserIdAsync(TestConstants.ValidUserId, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<TaskItem>)[], 0));
        var sut = BuildService(repo);

        await sut.GetAllTasksAsync(TestConstants.ValidUserId, 2, 10, CancellationToken.None);

        repo.Verify(r => r.GetPagedByUserIdAsync(TestConstants.ValidUserId, 2, 10, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetAllTasksAsync_ShouldReturnValidationFailure_WhenPageIsZero()
    {
        var repo = new Mock<ITaskRepository>();
        var sut = BuildService(repo);

        var result = await sut.GetAllTasksAsync(TestConstants.ValidUserId, 0, 20, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        repo.Verify(r => r.GetPagedByUserIdAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task GetAllTasksAsync_ShouldReturnValidationFailure_WhenPageIsNegative()
    {
        var repo = new Mock<ITaskRepository>();
        var sut = BuildService(repo);

        var result = await sut.GetAllTasksAsync(TestConstants.ValidUserId, -1, 20, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetAllTasksAsync_ShouldReturnValidationFailure_WhenPageSizeIsZero()
    {
        var repo = new Mock<ITaskRepository>();
        var sut = BuildService(repo);

        var result = await sut.GetAllTasksAsync(TestConstants.ValidUserId, 1, 0, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetAllTasksAsync_ShouldReturnValidationFailure_WhenPageSizeExceedsMaximum()
    {
        var repo = new Mock<ITaskRepository>();
        var sut = BuildService(repo);

        var result = await sut.GetAllTasksAsync(TestConstants.ValidUserId, 1, 101, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetAllTasksAsync_ShouldAcceptPageSize100_AsValidBoundary()
    {
        var repo = new Mock<ITaskRepository>();
        repo.Setup(r => r.GetPagedByUserIdAsync(TestConstants.ValidUserId, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<TaskItem>)[], 0));
        var sut = BuildService(repo);

        var result = await sut.GetAllTasksAsync(TestConstants.ValidUserId, 1, 100, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetAllTasksAsync_ShouldReturnEmptyPagedResult_WhenRepositoryReturnsNoItems()
    {
        var repo = new Mock<ITaskRepository>();
        repo.Setup(r => r.GetPagedByUserIdAsync(TestConstants.ValidUserId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<TaskItem>)[], 0));
        var sut = BuildService(repo);

        var result = await sut.GetAllTasksAsync(TestConstants.ValidUserId, 1, 20, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Empty(result.Value.Items);
    }
}
