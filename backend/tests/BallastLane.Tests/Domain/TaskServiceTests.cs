using BallastLane.Application.Services;
using BallastLane.Application.Validators;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using BallastLane.Tests.TestData;
using Moq;

namespace BallastLane.Tests.Domain;

public sealed class TaskServiceTests
{
    private static (TaskService Sut, Mock<ITaskRepository> Repo, Mock<IUnitOfWork> Uow) BuildSut(
        Mock<IDateTimeProvider> clock)
    {
        var repo = new Mock<ITaskRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Tasks).Returns(repo.Object);
        var sut = new TaskService(uow.Object, new TaskValidator(), clock.Object);
        return (sut, repo, uow);
    }

    [Fact]
    public async Task CreateTask_ShouldReturnTaskWithGeneratedId_WhenAllFieldsAreValid()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var (sut, _, _) = BuildSut(clock);
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
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var (sut, repo, _) = BuildSut(clock);
        var request = TestDataBuilder.ValidCreateRequest();

        await sut.CreateTaskAsync(request, TestConstants.ValidUserId);

        repo.Verify(
            r => r.SaveAsync(
                It.Is<TaskItem>(t => t.Title == TestConstants.ValidTitle && t.UserId == TestConstants.ValidUserId),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task CreateTask_ShouldReturnFailure_WhenTitleIsEmpty()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var (sut, _, _) = BuildSut(clock);
        var request = TestDataBuilder.ValidCreateRequest(title: string.Empty);

        var result = await sut.CreateTaskAsync(request, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("Title is required"));
    }

    [Fact]
    public async Task CreateTask_ShouldReturnFailure_WhenTitleExceeds200Characters()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var (sut, _, _) = BuildSut(clock);
        var request = TestDataBuilder.ValidCreateRequest(title: TestConstants.LongTitle);

        var result = await sut.CreateTaskAsync(request, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("200"));
    }

    [Fact]
    public async Task CreateTask_ShouldReturnFailure_WhenDueDateIsInThePast()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var (sut, _, _) = BuildSut(clock);
        var request = TestDataBuilder.ValidCreateRequest(dueDate: TestConstants.PastDueDate);

        var result = await sut.CreateTaskAsync(request, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("Due date must be in the future"));
    }

    [Fact]
    public async Task CreateTask_ShouldReturnFailure_WhenDueDateIsLaterTodayNotTomorrow()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var (sut, _, _) = BuildSut(clock);
        // DueDate = same UTC date as now but 3 hours later (today, not a future calendar day)
        var request = TestDataBuilder.ValidCreateRequest(
            dueDate: TestConstants.FixedUtcNow.AddHours(3));

        var result = await sut.CreateTaskAsync(request, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("Due date must be in the future"));
    }

    [Fact]
    public async Task CreateTask_ShouldReturnFailure_WhenDescriptionIsEmpty()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var (sut, _, _) = BuildSut(clock);
        var request = TestDataBuilder.ValidCreateRequest(description: string.Empty);

        var result = await sut.CreateTaskAsync(request, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("Description is required"));
    }

    [Fact]
    public async Task GetTaskById_ShouldReturnTask_WhenTaskBelongsToRequestingUser()
    {
        var clock = new Mock<IDateTimeProvider>();
        var (sut, repo, _) = BuildSut(clock);
        var existingTask = TestDataBuilder.ValidTask(id: TestConstants.ValidTaskId, userId: TestConstants.ValidUserId);
        repo
            .Setup(r => r.GetByIdAsync(TestConstants.ValidTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);

        var result = await sut.GetTaskByIdAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestConstants.ValidTaskId, result.Value.Id);
    }

    [Fact]
    public async Task GetTaskById_ShouldReturnFailure_WhenTaskDoesNotExist()
    {
        var clock = new Mock<IDateTimeProvider>();
        var (sut, repo, _) = BuildSut(clock);
        repo
            .Setup(r => r.GetByIdAsync(TestConstants.UnknownTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var result = await sut.GetTaskByIdAsync(TestConstants.UnknownTaskId, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains(TestConstants.UnknownTaskId.ToString()));
    }

    [Fact]
    public async Task GetTaskById_ShouldReturnFailure_WhenTaskBelongsToDifferentUser()
    {
        var clock = new Mock<IDateTimeProvider>();
        var (sut, repo, _) = BuildSut(clock);
        var taskOwnedByOther = TestDataBuilder.ValidTask(id: TestConstants.ValidTaskId, userId: TestConstants.OtherUserId);
        repo
            .Setup(r => r.GetByIdAsync(TestConstants.ValidTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taskOwnedByOther);

        var result = await sut.GetTaskByIdAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains("Access denied"));
    }

    [Fact]
    public async Task UpdateTask_ShouldModifyFields_WhenRequestIsValid()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var (sut, repo, _) = BuildSut(clock);
        var existingTask = TestDataBuilder.ValidTask(id: TestConstants.ValidTaskId, userId: TestConstants.ValidUserId);
        repo
            .Setup(r => r.GetByIdAsync(TestConstants.ValidTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);
        var updateRequest = TestDataBuilder.ValidUpdateRequest();

        var result = await sut.UpdateTaskAsync(TestConstants.ValidTaskId, updateRequest, TestConstants.ValidUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(updateRequest.Title, result.Value.Title);
        Assert.Equal(updateRequest.Status, result.Value.Status);
        repo.Verify(
            r => r.UpdateAsync(
                It.Is<TaskItem>(t => t.Id == TestConstants.ValidTaskId && t.Title == updateRequest.Title),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task UpdateTask_ShouldReturnFailure_WhenTaskNotFound()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestConstants.FixedUtcNow);
        var (sut, repo, _) = BuildSut(clock);
        repo
            .Setup(r => r.GetByIdAsync(TestConstants.UnknownTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);
        var updateRequest = TestDataBuilder.ValidUpdateRequest();

        var result = await sut.UpdateTaskAsync(TestConstants.UnknownTaskId, updateRequest, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains(TestConstants.UnknownTaskId.ToString()));
    }

    [Fact]
    public async Task DeleteTask_ShouldSucceed_WhenTaskBelongsToUser()
    {
        var clock = new Mock<IDateTimeProvider>();
        var (sut, repo, _) = BuildSut(clock);
        var existingTask = TestDataBuilder.ValidTask(id: TestConstants.ValidTaskId, userId: TestConstants.ValidUserId);
        repo
            .Setup(r => r.GetByIdAsync(TestConstants.ValidTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);

        var result = await sut.DeleteTaskAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId);

        Assert.True(result.IsSuccess);
        repo.Verify(
            r => r.DeleteAsync(TestConstants.ValidTaskId, It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task DeleteTask_ShouldReturnFailure_WhenTaskNotFound()
    {
        var clock = new Mock<IDateTimeProvider>();
        var (sut, repo, _) = BuildSut(clock);
        repo
            .Setup(r => r.GetByIdAsync(TestConstants.UnknownTaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var result = await sut.DeleteTaskAsync(TestConstants.UnknownTaskId, TestConstants.ValidUserId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Contains(TestConstants.UnknownTaskId.ToString()));
    }

    [Fact]
    public async Task GetAllTasks_ShouldReturnOnlyTasksOwnedByUser()
    {
        var clock = new Mock<IDateTimeProvider>();
        var (sut, repo, _) = BuildSut(clock);
        var userTasks = new List<TaskItem>
        {
            TestDataBuilder.ValidTask(id: TestConstants.ValidTaskId, userId: TestConstants.ValidUserId),
            TestDataBuilder.ValidTask(id: Guid.NewGuid(), userId: TestConstants.ValidUserId)
        };
        repo
            .Setup(r => r.GetAllByUserIdAsync(TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userTasks);

        var result = await sut.GetAllTasksAsync(TestConstants.ValidUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, t => Assert.Equal(TestConstants.ValidUserId, t.UserId));
    }
}
