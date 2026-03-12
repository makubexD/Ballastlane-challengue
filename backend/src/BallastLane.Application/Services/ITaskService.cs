using BallastLane.Application.DTOs;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;

namespace BallastLane.Application.Services;

public interface ITaskService
{
    Task<Result<TaskItem>> CreateTaskAsync(CreateTaskRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<TaskItem>> GetTaskByIdAsync(Guid id, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TaskItem>>> GetAllTasksAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<TaskItem>> UpdateTaskAsync(Guid id, UpdateTaskRequest request, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<Result> DeleteTaskAsync(Guid id, Guid requestingUserId, CancellationToken cancellationToken = default);
}
