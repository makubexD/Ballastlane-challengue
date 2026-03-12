using BallastLane.Application.DTOs;
using BallastLane.Application.Validators;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;

namespace BallastLane.Application.Services;

public sealed class TaskService(
    ITaskRepository taskRepository,
    TaskValidator validator,
    IDateTimeProvider clock) : ITaskService
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
        return Result<TaskItem>.Ok(task);
    }

    public async Task<Result<TaskItem>> GetTaskByIdAsync(
        Guid id,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(id, cancellationToken);

        if (task is null)
            return Result<TaskItem>.Fail($"Task with id '{id}' was not found.");

        if (task.UserId != requestingUserId)
            return Result<TaskItem>.Fail("Access denied.");

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
            return Result<TaskItem>.Fail($"Task with id '{id}' was not found.");

        if (existing.UserId != requestingUserId)
            return Result<TaskItem>.Fail("Access denied.");

        var updated = TaskItem.Create(existing.Id, request.Title, request.Description, request.Status, request.DueDate, existing.UserId);
        await taskRepository.UpdateAsync(updated, cancellationToken);
        return Result<TaskItem>.Ok(updated);
    }

    public async Task<Result> DeleteTaskAsync(
        Guid id,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await taskRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return Result.Fail($"Task with id '{id}' was not found.");

        if (existing.UserId != requestingUserId)
            return Result.Fail("Access denied.");

        await taskRepository.DeleteAsync(id, cancellationToken);
        return Result.Ok();
    }
}
