using BallastLane.Application.Services;
using BallastLane.Application.Validators;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using BallastLane.Tests.TestData;
using Moq;

namespace BallastLane.Tests.Domain;

public sealed class TaskServiceTests
{
    [Fact]
    public async Task CreateTask_ShouldReturnTaskWithGeneratedId_WhenAllFieldsAreValid()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);
        var request = TestDataBuilder.ValidCreateRequest();

        var result = await sut.CreateTaskAsync(request, TestConstants.ValidUserId);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(TestConstants.ValidTitle, result.Value.Title);
        Assert.Equal(TestConstants.ValidUserId, result.Value.UserId);
    }

    [Fact]
    public async Task CreateTask_ShouldCallRepositorySave_WhenRequestIsValid()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);
        var request = TestDataBuilder.ValidCreateRequest();

        await sut.CreateTaskAsync(request, TestConstants.ValidUserId);

        repository.Verify(
            r => r.SaveAsync(
                It.Is<TaskItem>(t => t.Title == TestConstants.ValidTitle && t.UserId == TestConstants.ValidUserId),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task CreateTask_ShouldReturnFailure_WhenTitleIsEmpty()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);
        var request = TestDataBuilder.ValidCreateRequest(title: string.Empty);

        var result = await sut.CreateTaskAsync(request, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("Title is required"));
    }

    [Fact]
    public async Task CreateTask_ShouldReturnFailure_WhenTitleExceeds200Characters()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);
        var request = TestDataBuilder.ValidCreateRequest(title: TestConstants.LongTitle);

        var result = await sut.CreateTaskAsync(request, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("200"));
    }

    [Fact]
    public async Task CreateTask_ShouldReturnFailure_WhenDueDateIsInThePast()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);
        var request = TestDataBuilder.ValidCreateRequest(dueDate: TestConstants.PastDueDate);

        var result = await sut.CreateTaskAsync(request, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("Due date must be in the future"));
    }

    [Fact]
    public async Task CreateTask_ShouldReturnFailure_WhenDescriptionIsEmpty()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);
        var request = TestDataBuilder.ValidCreateRequest(description: string.Empty);

        var result = await sut.CreateTaskAsync(request, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("Description is required"));
    }

    [Fact]
    public async Task GetTaskById_ShouldReturnTask_WhenTaskBelongsToRequestingUser()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        var existingTask = TestDataBuilder.ValidTask(id: TestConstants.ValidTaskId, userId: TestConstants.ValidUserId);
        repository
            .Setup(r => r.GetByIdAsync(TestConstants.ValidTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);

        var result = await sut.GetTaskByIdAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestConstants.ValidTaskId, result.Value.Id);
    }

    [Fact]
    public async Task GetTaskById_ShouldReturnFailure_WhenTaskDoesNotExist()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        repository
            .Setup(r => r.GetByIdAsync(TestConstants.UnknownTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);

        var result = await sut.GetTaskByIdAsync(TestConstants.UnknownTaskId, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains(TestConstants.UnknownTaskId.ToString()));
    }

    [Fact]
    public async Task GetTaskById_ShouldReturnFailure_WhenTaskBelongsToDifferentUser()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        var taskOwnedByOther = TestDataBuilder.ValidTask(id: TestConstants.ValidTaskId, userId: TestConstants.OtherUserId);
        repository
            .Setup(r => r.GetByIdAsync(TestConstants.ValidTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taskOwnedByOther);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);

        var result = await sut.GetTaskByIdAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("Access denied"));
    }

    [Fact]
    public async Task UpdateTask_ShouldModifyFields_WhenRequestIsValid()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var existingTask = TestDataBuilder.ValidTask(id: TestConstants.ValidTaskId, userId: TestConstants.ValidUserId);
        repository
            .Setup(r => r.GetByIdAsync(TestConstants.ValidTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);
        var updateRequest = TestDataBuilder.ValidUpdateRequest();

        var result = await sut.UpdateTaskAsync(TestConstants.ValidTaskId, updateRequest, TestConstants.ValidUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(updateRequest.Title, result.Value.Title);
        Assert.Equal(updateRequest.Status, result.Value.Status);
        repository.Verify(
            r => r.UpdateAsync(
                It.Is<TaskItem>(t => t.Id == TestConstants.ValidTaskId && t.Title == updateRequest.Title),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task UpdateTask_ShouldReturnFailure_WhenTaskNotFound()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        repository
            .Setup(r => r.GetByIdAsync(TestConstants.UnknownTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);
        var updateRequest = TestDataBuilder.ValidUpdateRequest();

        var result = await sut.UpdateTaskAsync(TestConstants.UnknownTaskId, updateRequest, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains(TestConstants.UnknownTaskId.ToString()));
    }

    [Fact]
    public async Task DeleteTask_ShouldSucceed_WhenTaskBelongsToUser()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        var existingTask = TestDataBuilder.ValidTask(id: TestConstants.ValidTaskId, userId: TestConstants.ValidUserId);
        repository
            .Setup(r => r.GetByIdAsync(TestConstants.ValidTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);

        var result = await sut.DeleteTaskAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId);

        Assert.True(result.IsSuccess);
        repository.Verify(
            r => r.DeleteAsync(TestConstants.ValidTaskId, It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task DeleteTask_ShouldReturnFailure_WhenTaskNotFound()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        repository
            .Setup(r => r.GetByIdAsync(TestConstants.UnknownTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);

        var result = await sut.DeleteTaskAsync(TestConstants.UnknownTaskId, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains(TestConstants.UnknownTaskId.ToString()));
    }

    [Fact]
    public async Task GetAllTasks_ShouldReturnOnlyTasksOwnedByUser()
    {
        var repository = new Mock<ITaskRepository>();
        var clock = new Mock<IDateTimeProvider>();
        var userTasks = new List<TaskItem>
        {
            TestDataBuilder.ValidTask(id: TestConstants.ValidTaskId, userId: TestConstants.ValidUserId),
            TestDataBuilder.ValidTask(id: Guid.NewGuid(), userId: TestConstants.ValidUserId)
        };
        repository
            .Setup(r => r.GetAllByUserIdAsync(TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userTasks);
        var sut = new TaskService(repository.Object, new TaskValidator(), clock.Object);

        var result = await sut.GetAllTasksAsync(TestConstants.ValidUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, t => Assert.Equal(TestConstants.ValidUserId, t.UserId));
    }
}
