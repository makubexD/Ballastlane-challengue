namespace BallastLane.Domain.Interfaces;

/// <summary>
/// Coordinates a single database transaction across multiple repositories.
/// One instance per HTTP request (scoped lifetime).
/// Call <see cref="BeginAsync"/> before performing any repository operations,
/// then <see cref="CommitAsync"/> on success or <see cref="RollbackAsync"/> on failure.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>Opens the database connection and begins a transaction.</summary>
    Task BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>Commits all changes made during this unit of work.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back all changes made during this unit of work.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
