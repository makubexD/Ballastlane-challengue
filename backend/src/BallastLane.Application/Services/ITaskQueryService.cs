using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;

namespace BallastLane.Application.Services;

public interface ITaskQueryService
{
    Task<Result<TaskItem>> GetTaskByIdAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TaskItem>>> GetAllTasksAsync(Guid userId, CancellationToken cancellationToken = default);
}
