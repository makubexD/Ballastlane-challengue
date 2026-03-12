using BallastLane.Application.Common;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;

namespace BallastLane.Application.Services;

public interface ITaskQueryService
{
    Task<Result<TaskItem>> GetTaskByIdAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<TaskItem>>> GetAllTasksAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
}
