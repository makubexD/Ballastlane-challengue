using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using Npgsql;

namespace BallastLane.Infrastructure.Persistence;

/// <summary>
/// ITaskRepository implementation that participates in a shared NpgsqlTransaction.
/// All operations execute on the connection and transaction provided at construction time.
/// </summary>
internal sealed class TransactionalTaskRepository(
    NpgsqlConnection connection,
    NpgsqlTransaction transaction,
    IDateTimeProvider dateTimeProvider) : ITaskRepository
{
    public async Task SaveAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await using var command = new NpgsqlCommand(TaskSql.Insert, connection, transaction);
        TaskSql.AddInsertParameters(command, task);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var command = new NpgsqlCommand(TaskSql.SelectById, connection, transaction);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? TaskSql.MapToTask(reader) : null;
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var command = new NpgsqlCommand(TaskSql.SelectAllByUserId, connection, transaction);
        command.Parameters.AddWithValue("@user_id", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var tasks = new List<TaskItem>();
        while (await reader.ReadAsync(cancellationToken))
            tasks.Add(TaskSql.MapToTask(reader));

        return tasks;
    }

    public async Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var countCommand = new NpgsqlCommand(TaskSql.CountByUserId, connection, transaction);
        countCommand.Parameters.AddWithValue("@user_id", userId);
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        var offset = (page - 1) * pageSize;
        await using var selectCommand = new NpgsqlCommand(TaskSql.SelectPagedByUserId, connection, transaction);
        selectCommand.Parameters.AddWithValue("@user_id", userId);
        selectCommand.Parameters.AddWithValue("@page_size", pageSize);
        selectCommand.Parameters.AddWithValue("@offset", offset);

        await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
        var items = new List<TaskItem>();
        while (await reader.ReadAsync(cancellationToken))
            items.Add(TaskSql.MapToTask(reader));

        return (items, totalCount);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await using var command = new NpgsqlCommand(TaskSql.Update, connection, transaction);
        command.Parameters.AddWithValue("@id", task.Id);
        command.Parameters.AddWithValue("@title", task.Title);
        command.Parameters.AddWithValue("@description", task.Description);
        command.Parameters.AddWithValue("@status", task.Status.ToString());
        command.Parameters.AddWithValue("@due_date", task.DueDate);
        command.Parameters.AddWithValue("@updated_at", dateTimeProvider.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var command = new NpgsqlCommand(TaskSql.Delete, connection, transaction);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
