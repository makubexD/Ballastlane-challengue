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
    private static readonly string[] TitleRequiredErrors = [TitleRequiredError];

    private static TasksController BuildController(
        Mock<ITaskCommandService> commandService,
        Mock<ITaskQueryService> queryService,
        Mock<ICurrentUserService> currentUser)
        => new(commandService.Object, queryService.Object, currentUser.Object);

    [Fact]
    public async Task GetAll_ShouldReturn200WithTasks_WhenUserHasTasks()
    {
        var commandService = new Mock<ITaskCommandService>();
        var queryService = new Mock<ITaskQueryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var tasks = (IReadOnlyList<TaskItem>)[TestDataBuilder.ValidTask()];
        queryService
            .Setup(s => s.GetAllTasksAsync(TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskItem>>.Ok(tasks));
        var sut = BuildController(commandService, queryService, currentUser);

        var result = await sut.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tasks, ok.Value);
    }

    [Fact]
    public async Task GetAll_ShouldReturn200WithEmptyList_WhenUserHasNoTasks()
    {
        var commandService = new Mock<ITaskCommandService>();
        var queryService = new Mock<ITaskQueryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        IReadOnlyList<TaskItem> emptyList = [];
        queryService
            .Setup(s => s.GetAllTasksAsync(TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskItem>>.Ok(emptyList));
        var sut = BuildController(commandService, queryService, currentUser);

        var result = await sut.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(emptyList, ok.Value);
    }

    [Fact]
    public async Task GetAll_ShouldInvokeQueryService_NotCommandService()
    {
        var commandService = new Mock<ITaskCommandService>();
        var queryService = new Mock<ITaskQueryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        IReadOnlyList<TaskItem> tasks = [TestDataBuilder.ValidTask()];
        queryService
            .Setup(s => s.GetAllTasksAsync(TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskItem>>.Ok(tasks));
        var sut = BuildController(commandService, queryService, currentUser);

        await sut.GetAll(CancellationToken.None);

        queryService.Verify(
            s => s.GetAllTasksAsync(TestConstants.ValidUserId, It.IsAny<CancellationToken>()),
            Times.Once());
        commandService.Verify(
            s => s.CreateTaskAsync(It.IsAny<CreateTaskRequest>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never());
        commandService.Verify(
            s => s.UpdateTaskAsync(It.IsAny<Guid>(), It.IsAny<UpdateTaskRequest>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never());
        commandService.Verify(
            s => s.DeleteTaskAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task GetById_ShouldReturn200WithTask_WhenTaskExists()
    {
        var commandService = new Mock<ITaskCommandService>();
        var queryService = new Mock<ITaskQueryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var task = TestDataBuilder.ValidTask();
        queryService
            .Setup(s => s.GetTaskByIdAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var sut = BuildController(commandService, queryService, currentUser);

        var result = await sut.GetById(TestConstants.ValidTaskId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(task, ok.Value);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenTaskNotFound()
    {
        var commandService = new Mock<ITaskCommandService>();
        var queryService = new Mock<ITaskQueryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        queryService
            .Setup(s => s.GetTaskByIdAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail("Task was not found.", ResultErrorType.NotFound));
        var sut = BuildController(commandService, queryService, currentUser);

        var result = await sut.GetById(TestConstants.ValidTaskId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ShouldReturn401_WhenAccessDenied()
    {
        var commandService = new Mock<ITaskCommandService>();
        var queryService = new Mock<ITaskQueryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        queryService
            .Setup(s => s.GetTaskByIdAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail("Access denied.", ResultErrorType.Unauthorized));
        var sut = BuildController(commandService, queryService, currentUser);

        var result = await sut.GetById(TestConstants.ValidTaskId, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Create_ShouldReturn201WithTask_WhenRequestIsValid()
    {
        var commandService = new Mock<ITaskCommandService>();
        var queryService = new Mock<ITaskQueryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var task = TestDataBuilder.ValidTask();
        var request = TestDataBuilder.ValidCreateRequest();
        commandService
            .Setup(s => s.CreateTaskAsync(request, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var sut = BuildController(commandService, queryService, currentUser);

        var result = await sut.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(TestConstants.ValidTaskId, ((TaskItem)created.Value!).Id);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenTitleIsEmpty()
    {
        var commandService = new Mock<ITaskCommandService>();
        var queryService = new Mock<ITaskQueryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var request = TestDataBuilder.ValidCreateRequest(title: string.Empty);
        commandService
            .Setup(s => s.CreateTaskAsync(request, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail(TitleRequiredErrors));
        var sut = BuildController(commandService, queryService, currentUser);

        var result = await sut.Create(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_ShouldReturn200WithUpdatedTask_WhenRequestIsValid()
    {
        var commandService = new Mock<ITaskCommandService>();
        var queryService = new Mock<ITaskQueryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var task = TestDataBuilder.ValidTask();
        var request = TestDataBuilder.ValidUpdateRequest();
        commandService
            .Setup(s => s.UpdateTaskAsync(TestConstants.ValidTaskId, request, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var sut = BuildController(commandService, queryService, currentUser);

        var result = await sut.Update(TestConstants.ValidTaskId, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(task, ok.Value);
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenTaskNotFound()
    {
        var commandService = new Mock<ITaskCommandService>();
        var queryService = new Mock<ITaskQueryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var request = TestDataBuilder.ValidUpdateRequest();
        commandService
            .Setup(s => s.UpdateTaskAsync(TestConstants.ValidTaskId, request, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail("Task was not found.", ResultErrorType.NotFound));
        var sut = BuildController(commandService, queryService, currentUser);

        var result = await sut.Update(TestConstants.ValidTaskId, request, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_ShouldReturn400_WhenValidationFails()
    {
        var commandService = new Mock<ITaskCommandService>();
        var queryService = new Mock<ITaskQueryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var request = TestDataBuilder.ValidUpdateRequest(title: string.Empty);
        commandService
            .Setup(s => s.UpdateTaskAsync(TestConstants.ValidTaskId, request, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail(TitleRequiredErrors));
        var sut = BuildController(commandService, queryService, currentUser);

        var result = await sut.Update(TestConstants.ValidTaskId, request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturn204_WhenTaskDeleted()
    {
        var commandService = new Mock<ITaskCommandService>();
        var queryService = new Mock<ITaskQueryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        commandService
            .Setup(s => s.DeleteTaskAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var sut = BuildController(commandService, queryService, currentUser);

        var result = await sut.Delete(TestConstants.ValidTaskId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenTaskNotFound()
    {
        var commandService = new Mock<ITaskCommandService>();
        var queryService = new Mock<ITaskQueryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        commandService
            .Setup(s => s.DeleteTaskAsync(TestConstants.ValidTaskId, TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Task was not found.", ResultErrorType.NotFound));
        var sut = BuildController(commandService, queryService, currentUser);

        var result = await sut.Delete(TestConstants.ValidTaskId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
