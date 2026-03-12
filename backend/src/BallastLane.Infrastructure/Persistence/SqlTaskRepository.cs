using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using BallastLane.Infrastructure.Common;

namespace BallastLane.Infrastructure.Persistence;

public sealed class SqlTaskRepository(IDbConnectionFactory connectionFactory) : ITaskRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;

    public Task SaveAsync(TaskItem task, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Implemented in Phase 5.");

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Implemented in Phase 5.");

    public Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Implemented in Phase 5.");

    public Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Implemented in Phase 5.");

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Implemented in Phase 5.");
}
