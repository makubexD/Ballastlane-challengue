using BallastLane.Application.DTOs;
using BallastLane.Application.Events;
using BallastLane.Application.Validators;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Events;
using BallastLane.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BallastLane.Application.Services;

public sealed class TaskService(
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork,
    TaskValidator validator,
    IDateTimeProvider clock,
    ILogger<TaskService> logger,
    IDomainEventDispatcher eventDispatcher) : ITaskCommandService, ITaskQueryService
{
    public async Task<Result<TaskItem>> CreateTaskAsync(
        CreateTaskRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var errors = validator.Validate(request, clock.UtcNow);
        if (errors.Count > 0)
            return Result<TaskItem>.Fail(errors);

        var task = TaskItem.Create(Guid.NewGuid(), request.Title, request.Description, request.Status, request.DueDate, userId);
        await taskRepository.SaveAsync(task, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        logger.LogInformation("Task created: {TaskId} for user {UserId}", task.Id, userId);
        await eventDispatcher.DispatchAsync(new TaskCreatedEvent(task.Id, userId, task.Title, clock.UtcNow), cancellationToken);
        return Result<TaskItem>.Ok(task);
    }

    public async Task<Result<TaskItem>> GetTaskByIdAsync(
        Guid id,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(id, cancellationToken);

        if (task is null)
            return Result<TaskItem>.Fail($"Task with id '{id}' was not found.", ResultErrorType.NotFound);

        if (task.UserId != requestingUserId)
            return Result<TaskItem>.Fail("Access denied.", ResultErrorType.Unauthorized);

        return Result<TaskItem>.Ok(task);
    }

    public async Task<Result<IReadOnlyList<TaskItem>>> GetAllTasksAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tasks = await taskRepository.GetAllByUserIdAsync(userId, cancellationToken);
        return Result<IReadOnlyList<TaskItem>>.Ok(tasks);
    }

    public async Task<Result<TaskItem>> UpdateTaskAsync(
        Guid id,
        UpdateTaskRequest request,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var errors = validator.Validate(request, clock.UtcNow);
        if (errors.Count > 0)
            return Result<TaskItem>.Fail(errors);

        var existing = await taskRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return Result<TaskItem>.Fail($"Task with id '{id}' was not found.", ResultErrorType.NotFound);

        if (existing.UserId != requestingUserId)
            return Result<TaskItem>.Fail("Access denied.", ResultErrorType.Unauthorized);

        var updated = TaskItem.Create(existing.Id, request.Title, request.Description, request.Status, request.DueDate, existing.UserId);
        await taskRepository.UpdateAsync(updated, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        return Result<TaskItem>.Ok(updated);
    }

    public async Task<Result> DeleteTaskAsync(
        Guid id,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await taskRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return Result.Fail($"Task with id '{id}' was not found.", ResultErrorType.NotFound);

        if (existing.UserId != requestingUserId)
            return Result.Fail("Access denied.", ResultErrorType.Unauthorized);

        await taskRepository.DeleteAsync(id, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        logger.LogInformation("Task deleted: {TaskId} by user {UserId}", id, requestingUserId);
        await eventDispatcher.DispatchAsync(new TaskDeletedEvent(id, requestingUserId, clock.UtcNow), cancellationToken);
        return Result.Ok();
    }
}
