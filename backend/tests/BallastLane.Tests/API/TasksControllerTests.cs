using BallastLane.API.Controllers;
using BallastLane.API.Services;
using BallastLane.Application.CQRS;
using BallastLane.Application.Tasks.Commands;
using BallastLane.Application.Tasks.Queries;
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
        Mock<ICommandDispatcher> commandDispatcher,
        Mock<IQueryDispatcher> queryDispatcher,
        Mock<ICurrentUserService> currentUser)
        => new(commandDispatcher.Object, queryDispatcher.Object, currentUser.Object);

    [Fact]
    public async Task GetAll_ShouldReturn200WithTasks_WhenUserHasTasks()
    {
        var commandDispatcher = new Mock<ICommandDispatcher>();
        var queryDispatcher = new Mock<IQueryDispatcher>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var tasks = (IReadOnlyList<TaskItem>)[TestDataBuilder.ValidTask()];
        queryDispatcher
            .Setup(d => d.SendAsync<GetAllTasksQuery, IReadOnlyList<TaskItem>>(
                It.Is<GetAllTasksQuery>(q => q.UserId == TestConstants.ValidUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskItem>>.Ok(tasks));
        var sut = BuildController(commandDispatcher, queryDispatcher, currentUser);

        var result = await sut.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tasks, ok.Value);
    }

    [Fact]
    public async Task GetAll_ShouldReturn200WithEmptyList_WhenUserHasNoTasks()
    {
        var commandDispatcher = new Mock<ICommandDispatcher>();
        var queryDispatcher = new Mock<IQueryDispatcher>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        IReadOnlyList<TaskItem> emptyList = [];
        queryDispatcher
            .Setup(d => d.SendAsync<GetAllTasksQuery, IReadOnlyList<TaskItem>>(
                It.IsAny<GetAllTasksQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskItem>>.Ok(emptyList));
        var sut = BuildController(commandDispatcher, queryDispatcher, currentUser);

        var result = await sut.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(emptyList, ok.Value);
    }

    [Fact]
    public async Task GetAll_ShouldInvokeQueryDispatcher_NotCommandDispatcher()
    {
        var commandDispatcher = new Mock<ICommandDispatcher>();
        var queryDispatcher = new Mock<IQueryDispatcher>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        IReadOnlyList<TaskItem> tasks = [TestDataBuilder.ValidTask()];
        queryDispatcher
            .Setup(d => d.SendAsync<GetAllTasksQuery, IReadOnlyList<TaskItem>>(
                It.IsAny<GetAllTasksQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskItem>>.Ok(tasks));
        var sut = BuildController(commandDispatcher, queryDispatcher, currentUser);

        await sut.GetAll(CancellationToken.None);

        queryDispatcher.Verify(
            d => d.SendAsync<GetAllTasksQuery, IReadOnlyList<TaskItem>>(
                It.IsAny<GetAllTasksQuery>(),
                It.IsAny<CancellationToken>()),
            Times.Once());
        commandDispatcher.Verify(
            d => d.SendAsync<CreateTaskCommand, TaskItem>(
                It.IsAny<CreateTaskCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task GetById_ShouldReturn200WithTask_WhenTaskExists()
    {
        var commandDispatcher = new Mock<ICommandDispatcher>();
        var queryDispatcher = new Mock<IQueryDispatcher>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var task = TestDataBuilder.ValidTask();
        queryDispatcher
            .Setup(d => d.SendAsync<GetTaskByIdQuery, TaskItem>(
                It.Is<GetTaskByIdQuery>(q => q.TaskId == TestConstants.ValidTaskId && q.UserId == TestConstants.ValidUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var sut = BuildController(commandDispatcher, queryDispatcher, currentUser);

        var result = await sut.GetById(TestConstants.ValidTaskId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(task, ok.Value);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenTaskNotFound()
    {
        var commandDispatcher = new Mock<ICommandDispatcher>();
        var queryDispatcher = new Mock<IQueryDispatcher>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        queryDispatcher
            .Setup(d => d.SendAsync<GetTaskByIdQuery, TaskItem>(
                It.IsAny<GetTaskByIdQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail("Task was not found.", ResultErrorType.NotFound));
        var sut = BuildController(commandDispatcher, queryDispatcher, currentUser);

        var result = await sut.GetById(TestConstants.ValidTaskId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ShouldReturn401_WhenAccessDenied()
    {
        var commandDispatcher = new Mock<ICommandDispatcher>();
        var queryDispatcher = new Mock<IQueryDispatcher>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        queryDispatcher
            .Setup(d => d.SendAsync<GetTaskByIdQuery, TaskItem>(
                It.IsAny<GetTaskByIdQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail("Access denied.", ResultErrorType.Unauthorized));
        var sut = BuildController(commandDispatcher, queryDispatcher, currentUser);

        var result = await sut.GetById(TestConstants.ValidTaskId, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Create_ShouldReturn201WithTask_WhenRequestIsValid()
    {
        var commandDispatcher = new Mock<ICommandDispatcher>();
        var queryDispatcher = new Mock<IQueryDispatcher>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var task = TestDataBuilder.ValidTask();
        var request = TestDataBuilder.ValidCreateRequest();
        commandDispatcher
            .Setup(d => d.SendAsync<CreateTaskCommand, TaskItem>(
                It.Is<CreateTaskCommand>(c =>
                    c.Title == request.Title &&
                    c.UserId == TestConstants.ValidUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var sut = BuildController(commandDispatcher, queryDispatcher, currentUser);

        var result = await sut.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(TestConstants.ValidTaskId, ((TaskItem)created.Value!).Id);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenTitleIsEmpty()
    {
        var commandDispatcher = new Mock<ICommandDispatcher>();
        var queryDispatcher = new Mock<IQueryDispatcher>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var request = TestDataBuilder.ValidCreateRequest(title: string.Empty);
        commandDispatcher
            .Setup(d => d.SendAsync<CreateTaskCommand, TaskItem>(
                It.IsAny<CreateTaskCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail(TitleRequiredErrors));
        var sut = BuildController(commandDispatcher, queryDispatcher, currentUser);

        var result = await sut.Create(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_ShouldReturn200WithUpdatedTask_WhenRequestIsValid()
    {
        var commandDispatcher = new Mock<ICommandDispatcher>();
        var queryDispatcher = new Mock<IQueryDispatcher>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var task = TestDataBuilder.ValidTask();
        var request = TestDataBuilder.ValidUpdateRequest();
        commandDispatcher
            .Setup(d => d.SendAsync<UpdateTaskCommand, TaskItem>(
                It.Is<UpdateTaskCommand>(c =>
                    c.TaskId == TestConstants.ValidTaskId &&
                    c.Title == request.Title &&
                    c.UserId == TestConstants.ValidUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var sut = BuildController(commandDispatcher, queryDispatcher, currentUser);

        var result = await sut.Update(TestConstants.ValidTaskId, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(task, ok.Value);
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenTaskNotFound()
    {
        var commandDispatcher = new Mock<ICommandDispatcher>();
        var queryDispatcher = new Mock<IQueryDispatcher>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var request = TestDataBuilder.ValidUpdateRequest();
        commandDispatcher
            .Setup(d => d.SendAsync<UpdateTaskCommand, TaskItem>(
                It.IsAny<UpdateTaskCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail("Task was not found.", ResultErrorType.NotFound));
        var sut = BuildController(commandDispatcher, queryDispatcher, currentUser);

        var result = await sut.Update(TestConstants.ValidTaskId, request, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_ShouldReturn400_WhenValidationFails()
    {
        var commandDispatcher = new Mock<ICommandDispatcher>();
        var queryDispatcher = new Mock<IQueryDispatcher>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        var request = TestDataBuilder.ValidUpdateRequest(title: string.Empty);
        commandDispatcher
            .Setup(d => d.SendAsync<UpdateTaskCommand, TaskItem>(
                It.IsAny<UpdateTaskCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail(TitleRequiredErrors));
        var sut = BuildController(commandDispatcher, queryDispatcher, currentUser);

        var result = await sut.Update(TestConstants.ValidTaskId, request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturn204_WhenTaskDeleted()
    {
        var commandDispatcher = new Mock<ICommandDispatcher>();
        var queryDispatcher = new Mock<IQueryDispatcher>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        commandDispatcher
            .Setup(d => d.SendAsync(
                It.Is<DeleteTaskCommand>(c =>
                    c.TaskId == TestConstants.ValidTaskId &&
                    c.UserId == TestConstants.ValidUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var sut = BuildController(commandDispatcher, queryDispatcher, currentUser);

        var result = await sut.Delete(TestConstants.ValidTaskId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenTaskNotFound()
    {
        var commandDispatcher = new Mock<ICommandDispatcher>();
        var queryDispatcher = new Mock<IQueryDispatcher>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(TestConstants.ValidUserId);
        commandDispatcher
            .Setup(d => d.SendAsync(
                It.IsAny<DeleteTaskCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Task was not found.", ResultErrorType.NotFound));
        var sut = BuildController(commandDispatcher, queryDispatcher, currentUser);

        var result = await sut.Delete(TestConstants.ValidTaskId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
