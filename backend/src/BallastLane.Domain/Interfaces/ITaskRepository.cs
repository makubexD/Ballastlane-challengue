using BallastLane.Domain.Entities;

namespace BallastLane.Domain.Interfaces;

public interface ITaskRepository
{
    Task SaveAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
