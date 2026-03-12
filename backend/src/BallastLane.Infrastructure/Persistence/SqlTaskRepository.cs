using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using BallastLane.Infrastructure.Common;
using Npgsql;

namespace BallastLane.Infrastructure.Persistence;

public sealed class SqlTaskRepository(
    IDbConnectionFactory connectionFactory,
    IDateTimeProvider dateTimeProvider) : ITaskRepository
{
    public async Task SaveAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(TaskSql.Insert, connection);
        TaskSql.AddInsertParameters(command, task);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(TaskSql.SelectById, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? TaskSql.MapToTask(reader) : null;
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(TaskSql.SelectAllByUserId, connection);
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
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);

        await using var countCommand = new NpgsqlCommand(TaskSql.CountByUserId, connection);
        countCommand.Parameters.AddWithValue("@user_id", userId);
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        var offset = (page - 1) * pageSize;
        await using var selectCommand = new NpgsqlCommand(TaskSql.SelectPagedByUserId, connection);
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
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(TaskSql.Update, connection);
        command.Parameters.AddWithValue("@id", task.Id);
        command.Parameters.AddWithValue("@title", task.Title);
        command.Parameters.AddWithValue("@description", task.Description);
        command.Parameters.AddWithValue("@status", task.Status.ToString());
        command.Parameters.AddWithValue("@due_date", task.DueDate);
        command.Parameters.AddWithValue("@updated_at", dateTimeProvider.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(TaskSql.SoftDelete, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@user_id", userId);
        command.Parameters.AddWithValue("@deleted_at", dateTimeProvider.UtcNow);
        command.Parameters.AddWithValue("@updated_at", dateTimeProvider.UtcNow);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }
}
