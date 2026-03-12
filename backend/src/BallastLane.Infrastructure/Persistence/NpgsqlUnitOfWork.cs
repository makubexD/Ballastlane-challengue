using BallastLane.Domain.Interfaces;
using BallastLane.Infrastructure.Common;
using Npgsql;

namespace BallastLane.Infrastructure.Persistence;

/// <summary>
/// Opens a single NpgsqlConnection and begins a transaction on first repository access,
/// then exposes transactional repository instances that share that connection and transaction.
/// Scoped lifetime — one instance per HTTP request.
/// </summary>
public sealed class NpgsqlUnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;
    private ITaskRepository? _tasks;
    private IUserRepository? _users;
    private bool _disposed;

    public NpgsqlUnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public ITaskRepository Tasks => _tasks ?? throw new InvalidOperationException(
        "Unit of work has not been initialized. Call BeginAsync before accessing repositories.");

    public IUserRepository Users => _users ?? throw new InvalidOperationException(
        "Unit of work has not been initialized. Call BeginAsync before accessing repositories.");

    /// <summary>
    /// Opens the database connection and begins a transaction, initializing all repositories.
    /// Must be called before accessing Tasks or Users.
    /// </summary>
    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        _connection = (NpgsqlConnection)await _connectionFactory.CreateAsync(cancellationToken);
        _transaction = await _connection.BeginTransactionAsync(cancellationToken);
        _tasks = new TransactionalTaskRepository(_connection, _transaction);
        _users = new TransactionalUserRepository(_connection, _transaction);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await _transaction!.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await _transaction!.RollbackAsync(cancellationToken);
    }

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
