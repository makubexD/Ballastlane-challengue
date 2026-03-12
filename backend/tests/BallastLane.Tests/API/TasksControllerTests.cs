using BallastLane.API.Controllers;
using BallastLane.API.Services;
using BallastLane.Application.DTOs;
using BallastLane.Application.Services;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;
using BallastLane.Tests.TestData;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BallastLane.Tests.API;

public sealed class TasksControllerTests
{
    private const string TitleRequiredError = "Title is required.";
    private const string NotFoundError = "not found";
    private const string AccessDeniedError = "Access denied";
    private const string TaskNotFoundError = "task not found";
    private static readonly string[] TitleRequiredErrors = [TitleRequiredError];

    [Fact]
    public async Task GetAll_ShouldReturn200WithTasks_WhenUserHasTasks()
    {
        var taskService = new Mock<ITaskService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var tasks = (IReadOnlyList<TaskItem>)[TestDataBuilder.ValidTask()];
        taskService
            .Setup(s => s.GetAllTasksAsync(TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskItem>>.Ok(tasks));
        var sut = new TasksController(taskService.Object, currentUser.Object);

        var result = await sut.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tasks, ok.Value);
    }

    [Fact]
    public async Task GetAll_ShouldReturn200WithEmptyList_WhenUserHasNoTasks()
    {
        var taskService = new Mock<ITaskService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        IReadOnlyList<TaskItem> emptyList = [];
        taskService
            .Setup(s => s.GetAllTasksAsync(TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskItem>>.Ok(emptyList));
        var sut = new TasksController(taskService.Object, currentUser.Object);

        var result = await sut.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(emptyList, ok.Value);
    }

    [Fact]
    public async Task GetById_ShouldReturn200WithTask_WhenTaskExists()
    {
        var taskService = new Mock<ITaskService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var task = TestDataBuilder.ValidTask();
        taskService
            .Setup(s => s.GetTaskByIdAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var sut = new TasksController(taskService.Object, currentUser.Object);

        var result = await sut.GetById(TestConstants.ValidTaskId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(task, ok.Value);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenTaskNotFound()
    {
        var taskService = new Mock<ITaskService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        taskService
            .Setup(s => s.GetTaskByIdAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail(NotFoundError));
        var sut = new TasksController(taskService.Object, currentUser.Object);

        var result = await sut.GetById(TestConstants.ValidTaskId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ShouldReturn400_WhenAccessDenied()
    {
        var taskService = new Mock<ITaskService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        taskService
            .Setup(s => s.GetTaskByIdAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail(AccessDeniedError));
        var sut = new TasksController(taskService.Object, currentUser.Object);

        var result = await sut.GetById(TestConstants.ValidTaskId, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_ShouldReturn201WithTask_WhenRequestIsValid()
    {
        var taskService = new Mock<ITaskService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var task = TestDataBuilder.ValidTask();
        var request = TestDataBuilder.ValidCreateRequest();
        taskService
            .Setup(s => s.CreateTaskAsync(request, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var sut = new TasksController(taskService.Object, currentUser.Object);

        var result = await sut.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(TestConstants.ValidTaskId, ((TaskItem)created.Value!).Id);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenTitleIsEmpty()
    {
        var taskService = new Mock<ITaskService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var request = TestDataBuilder.ValidCreateRequest(title: string.Empty);
        taskService
            .Setup(s => s.CreateTaskAsync(request, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail(TitleRequiredErrors));
        var sut = new TasksController(taskService.Object, currentUser.Object);

        var result = await sut.Create(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_ShouldReturn200WithUpdatedTask_WhenRequestIsValid()
    {
        var taskService = new Mock<ITaskService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var task = TestDataBuilder.ValidTask();
        var request = TestDataBuilder.ValidUpdateRequest();
        taskService
            .Setup(s => s.UpdateTaskAsync(TestConstants.ValidTaskId, request, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var sut = new TasksController(taskService.Object, currentUser.Object);

        var result = await sut.Update(TestConstants.ValidTaskId, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(task, ok.Value);
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenTaskNotFound()
    {
        var taskService = new Mock<ITaskService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var request = TestDataBuilder.ValidUpdateRequest();
        taskService
            .Setup(s => s.UpdateTaskAsync(TestConstants.ValidTaskId, request, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail(TaskNotFoundError));
        var sut = new TasksController(taskService.Object, currentUser.Object);

        var result = await sut.Update(TestConstants.ValidTaskId, request, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_ShouldReturn400_WhenValidationFails()
    {
        var taskService = new Mock<ITaskService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var request = TestDataBuilder.ValidUpdateRequest(title: string.Empty);
        taskService
            .Setup(s => s.UpdateTaskAsync(TestConstants.ValidTaskId, request, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail(TitleRequiredErrors));
        var sut = new TasksController(taskService.Object, currentUser.Object);

        var result = await sut.Update(TestConstants.ValidTaskId, request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturn204_WhenTaskDeleted()
    {
        var taskService = new Mock<ITaskService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        taskService
            .Setup(s => s.DeleteTaskAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var sut = new TasksController(taskService.Object, currentUser.Object);

        var result = await sut.Delete(TestConstants.ValidTaskId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenTaskNotFound()
    {
        var taskService = new Mock<ITaskService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        taskService
            .Setup(s => s.DeleteTaskAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(NotFoundError));
        var sut = new TasksController(taskService.Object, currentUser.Object);

        var result = await sut.Delete(TestConstants.ValidTaskId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
