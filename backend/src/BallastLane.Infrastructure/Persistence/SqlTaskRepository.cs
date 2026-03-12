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

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(TaskSql.Delete, connection);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
