using BallastLane.Domain.Interfaces;
using BallastLane.Infrastructure.Common;
using Microsoft.Data.Sqlite;

namespace BallastLane.Infrastructure.Persistence;

/// <summary>
/// Opens a single SqliteConnection and begins a transaction via <see cref="BeginAsync"/>,
/// then exposes transactional repository instances that share that connection and transaction.
/// Scoped lifetime — one instance per HTTP request.
/// </summary>
public sealed class SqliteUnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDateTimeProvider _dateTimeProvider;
    private SqliteConnection? _connection;
    private SqliteTransaction? _transaction;
    private ITaskRepository? _tasks;
    private bool _disposed;

    public SqliteUnitOfWork(IDbConnectionFactory connectionFactory, IDateTimeProvider dateTimeProvider)
    {
        _connectionFactory = connectionFactory;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>
    /// Exposes the transactional task repository for DI factory registration.
    /// Throws if <see cref="BeginAsync"/> has not been called.
    /// </summary>
    public ITaskRepository TaskRepository => _tasks ?? throw new InvalidOperationException(
        "Unit of work has not been initialized. Call BeginAsync before accessing repositories.");

    /// <inheritdoc/>
    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        _connection = (SqliteConnection)await _connectionFactory.CreateAsync(cancellationToken);
        _transaction = await _connection.BeginTransactionAsync(cancellationToken) as SqliteTransaction
            ?? throw new InvalidOperationException("Failed to begin SQLite transaction.");
        _tasks = new SqliteTransactionalTaskRepository(_connection, _transaction, _dateTimeProvider);
    }

    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await _transaction!.CommitAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await _transaction!.RollbackAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_transaction is not null)
            await _transaction.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();
    }

    private void EnsureInitialized()
    {
        if (_transaction is null)
            throw new InvalidOperationException(
                "Unit of work has not been initialized. Call BeginAsync before committing or rolling back.");
    }
}
