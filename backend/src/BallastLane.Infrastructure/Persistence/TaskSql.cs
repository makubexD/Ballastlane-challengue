using BallastLane.Domain.Entities;
using BallastLane.Domain.ValueObjects;
using Npgsql;

namespace BallastLane.Infrastructure.Persistence;

internal static class TaskSql
{
    internal const string Insert = """
        INSERT INTO tasks (id, title, description, status, due_date, user_id, created_at, updated_at)
        VALUES (@id, @title, @description, @status, @due_date, @user_id, @created_at, @updated_at);
        """;

    internal const string SelectById = """
        SELECT id, title, description, status, due_date, user_id, created_at, updated_at
        FROM tasks WHERE id = @id;
        """;

    internal const string SelectAllByUserId = """
        SELECT id, title, description, status, due_date, user_id, created_at, updated_at
        FROM tasks WHERE user_id = @user_id ORDER BY created_at DESC;
        """;

    internal const string Update = """
        UPDATE tasks
        SET title = @title, description = @description, status = @status,
            due_date = @due_date, updated_at = @updated_at
        WHERE id = @id;
        """;

    internal const string Delete = "DELETE FROM tasks WHERE id = @id;";

    internal const string CountByUserId = """
        SELECT COUNT(*) FROM tasks WHERE user_id = @user_id;
        """;

    internal const string SelectPagedByUserId = """
        SELECT id, title, description, status, due_date, user_id, created_at, updated_at
        FROM tasks
        WHERE user_id = @user_id
        ORDER BY created_at DESC
        LIMIT @page_size OFFSET @offset;
        """;

    internal const string ColId = "id";
    internal const string ColTitle = "title";
    internal const string ColDescription = "description";
    internal const string ColStatus = "status";
    internal const string ColDueDate = "due_date";
    internal const string ColUserId = "user_id";

    internal static TaskItem MapToTask(NpgsqlDataReader reader)
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

    internal static void AddInsertParameters(NpgsqlCommand command, TaskItem task)
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
}
