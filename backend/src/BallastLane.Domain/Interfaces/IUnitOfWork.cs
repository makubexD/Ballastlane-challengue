namespace BallastLane.Domain.Interfaces;

/// <summary>
/// Coordinates a single database transaction across multiple repositories.
/// One instance per HTTP request (scoped lifetime).
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    ITaskRepository Tasks { get; }
    IUserRepository Users { get; }

    /// <summary>Commits all changes made during this unit of work.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back all changes made during this unit of work.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
