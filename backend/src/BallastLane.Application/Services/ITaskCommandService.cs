using BallastLane.Application.DTOs;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;

namespace BallastLane.Application.Services;

public interface ITaskCommandService
{
    Task<Result<TaskItem>> CreateTaskAsync(CreateTaskRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<TaskItem>> UpdateTaskAsync(Guid taskId, UpdateTaskRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<Result> DeleteTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default);
}
