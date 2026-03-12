using System.Data;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using BallastLane.Domain.ValueObjects;
using BallastLane.Infrastructure.Common;
using Npgsql;

namespace BallastLane.Infrastructure.Persistence;

public sealed class SqlTaskRepository(IDbConnectionFactory connectionFactory) : ITaskRepository
{
    private const string InsertSql = """
        INSERT INTO tasks (id, title, description, status, due_date, user_id, created_at, updated_at)
        VALUES (@id, @title, @description, @status, @due_date, @user_id, @created_at, @updated_at);
        """;

    private const string SelectByIdSql = """
        SELECT id, title, description, status, due_date, user_id, created_at, updated_at
        FROM tasks WHERE id = @id;
        """;

    private const string SelectAllByUserIdSql = """
        SELECT id, title, description, status, due_date, user_id, created_at, updated_at
        FROM tasks WHERE user_id = @user_id ORDER BY created_at DESC;
        """;

    private const string UpdateSql = """
        UPDATE tasks
        SET title = @title, description = @description, status = @status,
            due_date = @due_date, updated_at = @updated_at
        WHERE id = @id;
        """;

    private const string DeleteSql = "DELETE FROM tasks WHERE id = @id;";

    private const string ColId = "id";
    private const string ColTitle = "title";
    private const string ColDescription = "description";
    private const string ColStatus = "status";
    private const string ColDueDate = "due_date";
    private const string ColUserId = "user_id";

    public async Task SaveAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(InsertSql, connection);
        AddInsertParameters(command, task);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(SelectByIdSql, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapToTaskItem(reader) : null;
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(SelectAllByUserIdSql, connection);
        command.Parameters.AddWithValue("@user_id", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var tasks = new List<TaskItem>();
        while (await reader.ReadAsync(cancellationToken))
            tasks.Add(MapToTaskItem(reader));

        return tasks;
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(UpdateSql, connection);
        command.Parameters.AddWithValue("@id", task.Id);
        command.Parameters.AddWithValue("@title", task.Title);
        command.Parameters.AddWithValue("@description", task.Description);
        command.Parameters.AddWithValue("@status", task.Status.ToString());
        command.Parameters.AddWithValue("@due_date", task.DueDate);
        command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(DeleteSql, connection);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddInsertParameters(NpgsqlCommand command, TaskItem task)
    {
        command.Parameters.AddWithValue("@id", task.Id);
        command.Parameters.AddWithValue("@title", task.Title);
        command.Parameters.AddWithValue("@description", task.Description);
        command.Parameters.AddWithValue("@status", task.Status.ToString());
        command.Parameters.AddWithValue("@due_date", task.DueDate);
        command.Parameters.AddWithValue("@user_id", task.UserId);
        command.Parameters.AddWithValue("@created_at", task.CreatedAt);
        command.Parameters.AddWithValue("@updated_at", task.UpdatedAt);
    }

    private static TaskItem MapToTaskItem(NpgsqlDataReader reader)
    {
        var status = Enum.Parse<TaskItemStatus>(reader.GetString(reader.GetOrdinal(ColStatus)));
        return TaskItem.Create(
            reader.GetGuid(reader.GetOrdinal(ColId)),
            reader.GetString(reader.GetOrdinal(ColTitle)),
            reader.GetString(reader.GetOrdinal(ColDescription)),
            status,
            reader.GetDateTime(reader.GetOrdinal(ColDueDate)).ToUniversalTime(),
            reader.GetGuid(reader.GetOrdinal(ColUserId)));
    }
}
