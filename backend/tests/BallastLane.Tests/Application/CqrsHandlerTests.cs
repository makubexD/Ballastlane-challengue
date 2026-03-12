using BallastLane.Application.CQRS;
using BallastLane.Application.DTOs;
using BallastLane.Application.Services;
using BallastLane.Application.Tasks.Commands;
using BallastLane.Application.Tasks.Handlers;
using BallastLane.Application.Tasks.Queries;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;
using BallastLane.Domain.ValueObjects;
using BallastLane.Tests.TestData;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BallastLane.Tests.Application;

public sealed class CqrsHandlerTests
{
    // ── CreateTaskCommandHandler ────────────────────────────────────────────

    [Fact]
    public async Task CreateTaskCommandHandler_ShouldDelegateToCommandService()
    {
        var commandService = new Mock<ITaskCommandService>();
        var task = TestDataBuilder.ValidTask();
        var expectedRequest = new CreateTaskRequest(
            TestConstants.ValidTitle,
            TestConstants.ValidDescription,
            TaskItemStatus.Todo,
            TestConstants.FutureDueDate);
        commandService
            .Setup(s => s.CreateTaskAsync(
                It.Is<CreateTaskRequest>(r =>
                    r.Title == expectedRequest.Title &&
                    r.Description == expectedRequest.Description &&
                    r.Status == expectedRequest.Status &&
                    r.DueDate == expectedRequest.DueDate),
                TestConstants.ValidUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var sut = new CreateTaskCommandHandler(commandService.Object);
        var command = new CreateTaskCommand(
            TestConstants.ValidTitle,
            TestConstants.ValidDescription,
            TaskItemStatus.Todo,
            TestConstants.FutureDueDate,
            TestConstants.ValidUserId);

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(task, result.Value);
    }

    [Fact]
    public async Task CreateTaskCommandHandler_ShouldReturnFailure_WhenServiceFails()
    {
        var commandService = new Mock<ITaskCommandService>();
        commandService
            .Setup(s => s.CreateTaskAsync(
                It.IsAny<CreateTaskRequest>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail("Title is required."));
        var sut = new CreateTaskCommandHandler(commandService.Object);
        var command = new CreateTaskCommand(string.Empty, string.Empty, TaskItemStatus.Todo, TestConstants.FutureDueDate, TestConstants.ValidUserId);

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── UpdateTaskCommandHandler ────────────────────────────────────────────

    [Fact]
    public async Task UpdateTaskCommandHandler_ShouldDelegateToCommandService()
    {
        var commandService = new Mock<ITaskCommandService>();
        var task = TestDataBuilder.ValidTask();
        commandService
            .Setup(s => s.UpdateTaskAsync(
                TestConstants.ValidTaskId,
                It.Is<UpdateTaskRequest>(r => r.Title == TestConstants.ValidTitle),
                TestConstants.ValidUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var sut = new UpdateTaskCommandHandler(commandService.Object);
        var command = new UpdateTaskCommand(
            TestConstants.ValidTaskId,
            TestConstants.ValidTitle,
            TestConstants.ValidDescription,
            TaskItemStatus.InProgress,
            TestConstants.FutureDueDate,
            TestConstants.ValidUserId);

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(task, result.Value);
    }

    [Fact]
    public async Task UpdateTaskCommandHandler_ShouldReturnNotFound_WhenTaskMissing()
    {
        var commandService = new Mock<ITaskCommandService>();
        commandService
            .Setup(s => s.UpdateTaskAsync(
                It.IsAny<Guid>(),
                It.IsAny<UpdateTaskRequest>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail("Task was not found.", ResultErrorType.NotFound));
        var sut = new UpdateTaskCommandHandler(commandService.Object);
        var command = new UpdateTaskCommand(
            TestConstants.UnknownTaskId,
            TestConstants.ValidTitle,
            TestConstants.ValidDescription,
            TaskItemStatus.Done,
            TestConstants.FutureDueDate,
            TestConstants.ValidUserId);

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    // ── DeleteTaskCommandHandler ────────────────────────────────────────────

    [Fact]
    public async Task DeleteTaskCommandHandler_ShouldDelegateToCommandService()
    {
        var commandService = new Mock<ITaskCommandService>();
        commandService
            .Setup(s => s.DeleteTaskAsync(
                TestConstants.ValidTaskId,
                TestConstants.ValidUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var sut = new DeleteTaskCommandHandler(commandService.Object);
        var command = new DeleteTaskCommand(TestConstants.ValidTaskId, TestConstants.ValidUserId);

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteTaskCommandHandler_ShouldReturnNotFound_WhenTaskMissing()
    {
        var commandService = new Mock<ITaskCommandService>();
        commandService
            .Setup(s => s.DeleteTaskAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Task was not found.", ResultErrorType.NotFound));
        var sut = new DeleteTaskCommandHandler(commandService.Object);
        var command = new DeleteTaskCommand(TestConstants.UnknownTaskId, TestConstants.ValidUserId);

        var result = await sut.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    // ── GetTaskByIdQueryHandler ─────────────────────────────────────────────

    [Fact]
    public async Task GetTaskByIdQueryHandler_ShouldDelegateToQueryService()
    {
        var queryService = new Mock<ITaskQueryService>();
        var task = TestDataBuilder.ValidTask();
        queryService
            .Setup(s => s.GetTaskByIdAsync(
                TestConstants.ValidTaskId,
                TestConstants.ValidUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var sut = new GetTaskByIdQueryHandler(queryService.Object);
        var query = new GetTaskByIdQuery(TestConstants.ValidTaskId, TestConstants.ValidUserId);

        var result = await sut.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(task, result.Value);
    }

    [Fact]
    public async Task GetTaskByIdQueryHandler_ShouldReturnUnauthorized_WhenAccessDenied()
    {
        var queryService = new Mock<ITaskQueryService>();
        queryService
            .Setup(s => s.GetTaskByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Fail("Access denied.", ResultErrorType.Unauthorized));
        var sut = new GetTaskByIdQueryHandler(queryService.Object);
        var query = new GetTaskByIdQuery(TestConstants.ValidTaskId, TestConstants.OtherUserId);

        var result = await sut.HandleAsync(query, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorType.Unauthorized, result.ErrorType);
    }

    // ── GetAllTasksQueryHandler ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllTasksQueryHandler_ShouldDelegateToQueryService()
    {
        var queryService = new Mock<ITaskQueryService>();
        var tasks = (IReadOnlyList<TaskItem>)[TestDataBuilder.ValidTask()];
        queryService
            .Setup(s => s.GetAllTasksAsync(TestConstants.ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskItem>>.Ok(tasks));
        var sut = new GetAllTasksQueryHandler(queryService.Object);
        var query = new GetAllTasksQuery(TestConstants.ValidUserId);

        var result = await sut.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(tasks, result.Value);
    }

    [Fact]
    public async Task GetAllTasksQueryHandler_ShouldReturnEmptyList_WhenUserHasNoTasks()
    {
        var queryService = new Mock<ITaskQueryService>();
        IReadOnlyList<TaskItem> emptyList = [];
        queryService
            .Setup(s => s.GetAllTasksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskItem>>.Ok(emptyList));
        var sut = new GetAllTasksQueryHandler(queryService.Object);
        var query = new GetAllTasksQuery(TestConstants.ValidUserId);

        var result = await sut.HandleAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    // ── CommandDispatcher ───────────────────────────────────────────────────

    [Fact]
    public async Task CommandDispatcher_SendAsync_Void_ShouldResolveHandlerAndDelegate()
    {
        var handler = new Mock<ICommandHandler<DeleteTaskCommand>>();
        handler
            .Setup(h => h.HandleAsync(It.IsAny<DeleteTaskCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var services = new ServiceCollection();
        services.AddSingleton(handler.Object);
        var sut = new BallastLane.Infrastructure.CQRS.CommandDispatcher(services.BuildServiceProvider());
        var command = new DeleteTaskCommand(TestConstants.ValidTaskId, TestConstants.ValidUserId);

        var result = await sut.SendAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        handler.Verify(h => h.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CommandDispatcher_SendAsync_WithResult_ShouldResolveHandlerAndDelegate()
    {
        var task = TestDataBuilder.ValidTask();
        var handler = new Mock<ICommandHandler<CreateTaskCommand, TaskItem>>();
        handler
            .Setup(h => h.HandleAsync(It.IsAny<CreateTaskCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var services = new ServiceCollection();
        services.AddSingleton(handler.Object);
        var sut = new BallastLane.Infrastructure.CQRS.CommandDispatcher(services.BuildServiceProvider());
        var command = new CreateTaskCommand(TestConstants.ValidTitle, TestConstants.ValidDescription, TaskItemStatus.Todo, TestConstants.FutureDueDate, TestConstants.ValidUserId);

        var result = await sut.SendAsync<CreateTaskCommand, TaskItem>(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(task, result.Value);
    }

    // ── QueryDispatcher ─────────────────────────────────────────────────────

    [Fact]
    public async Task QueryDispatcher_SendAsync_ShouldResolveHandlerAndDelegate()
    {
        var task = TestDataBuilder.ValidTask();
        var handler = new Mock<IQueryHandler<GetTaskByIdQuery, TaskItem>>();
        handler
            .Setup(h => h.HandleAsync(It.IsAny<GetTaskByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskItem>.Ok(task));
        var services = new ServiceCollection();
        services.AddSingleton(handler.Object);
        var sut = new BallastLane.Infrastructure.CQRS.QueryDispatcher(services.BuildServiceProvider());
        var query = new GetTaskByIdQuery(TestConstants.ValidTaskId, TestConstants.ValidUserId);

        var result = await sut.SendAsync<GetTaskByIdQuery, TaskItem>(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(task, result.Value);
    }
}
