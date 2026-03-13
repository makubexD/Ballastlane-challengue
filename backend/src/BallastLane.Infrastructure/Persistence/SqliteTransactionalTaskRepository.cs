using System.Globalization;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using BallastLane.Domain.ValueObjects;
using Microsoft.Data.Sqlite;

namespace BallastLane.Infrastructure.Persistence;

/// <summary>
/// ITaskRepository implementation that participates in a shared SqliteTransaction.
/// All operations execute on the connection and transaction provided at construction time.
/// GUIDs stored as TEXT (UUID dash format); DateTimes stored as ISO 8601 TEXT.
/// </summary>
internal sealed class SqliteTransactionalTaskRepository(
    SqliteConnection connection,
    SqliteTransaction transaction,
    IDateTimeProvider dateTimeProvider) : ITaskRepository
{
    public async Task SaveAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        using var cmd = new SqliteCommand(TaskSql.Insert, connection, transaction);
        AddInsertParameters(cmd, task);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var cmd = new SqliteCommand(TaskSql.SelectById, connection, transaction);
        cmd.Parameters.AddWithValue("@id", id.ToString("D"));
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapToTask(reader) : null;
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        using var cmd = new SqliteCommand(TaskSql.SelectAllByUserId, connection, transaction);
        cmd.Parameters.AddWithValue("@user_id", userId.ToString("D"));
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var tasks = new List<TaskItem>();
        while (await reader.ReadAsync(cancellationToken))
            tasks.Add(MapToTask(reader));
        return tasks;
    }

    public async Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        using var countCmd = new SqliteCommand(TaskSql.CountByUserId, connection, transaction);
        countCmd.Parameters.AddWithValue("@user_id", userId.ToString("D"));
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));

        var offset = (page - 1) * pageSize;
        using var selectCmd = new SqliteCommand(TaskSql.SelectPagedByUserId, connection, transaction);
        selectCmd.Parameters.AddWithValue("@user_id", userId.ToString("D"));
        selectCmd.Parameters.AddWithValue("@page_size", pageSize);
        selectCmd.Parameters.AddWithValue("@offset", offset);

        using var reader = await selectCmd.ExecuteReaderAsync(cancellationToken);
        var items = new List<TaskItem>();
        while (await reader.ReadAsync(cancellationToken))
            items.Add(MapToTask(reader));

        return (items, totalCount);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        using var cmd = new SqliteCommand(TaskSql.Update, connection, transaction);
        cmd.Parameters.AddWithValue("@id", task.Id.ToString("D"));
        cmd.Parameters.AddWithValue("@title", task.Title);
        cmd.Parameters.AddWithValue("@description", task.Description);
        cmd.Parameters.AddWithValue("@status", task.Status.ToString());
        cmd.Parameters.AddWithValue("@due_date", task.DueDate.ToString("O"));
        cmd.Parameters.AddWithValue("@updated_at", dateTimeProvider.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var now = dateTimeProvider.UtcNow.ToString("O");
        using var cmd = new SqliteCommand(TaskSql.SoftDelete, connection, transaction);
        cmd.Parameters.AddWithValue("@id", id.ToString("D"));
        cmd.Parameters.AddWithValue("@user_id", userId.ToString("D"));
        cmd.Parameters.AddWithValue("@deleted_at", now);
        cmd.Parameters.AddWithValue("@updated_at", now);
        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static TaskItem MapToTask(SqliteDataReader reader)
    {
        var idOrdinal = reader.GetOrdinal(TaskSql.ColId);
        var titleOrdinal = reader.GetOrdinal(TaskSql.ColTitle);
        var descOrdinal = reader.GetOrdinal(TaskSql.ColDescription);
        var statusOrdinal = reader.GetOrdinal(TaskSql.ColStatus);
        var dueDateOrdinal = reader.GetOrdinal(TaskSql.ColDueDate);
        var userIdOrdinal = reader.GetOrdinal(TaskSql.ColUserId);
        var createdAtOrdinal = reader.GetOrdinal(TaskSql.ColCreatedAt);
        var updatedAtOrdinal = reader.GetOrdinal(TaskSql.ColUpdatedAt);
        var deletedAtOrdinal = reader.GetOrdinal(TaskSql.ColDeletedAt);

        var status = Enum.Parse<TaskItemStatus>(reader.GetString(statusOrdinal));
        var createdAt = ParseDateTime(reader.GetString(createdAtOrdinal));
        var updatedAt = ParseDateTime(reader.GetString(updatedAtOrdinal));

        var task = TaskItem.Create(
            Guid.Parse(reader.GetString(idOrdinal)),
            reader.GetString(titleOrdinal),
            reader.GetString(descOrdinal),
            status,
            ParseDateTime(reader.GetString(dueDateOrdinal)),
            Guid.Parse(reader.GetString(userIdOrdinal)),
            createdAt,
            updatedAt);

        if (!reader.IsDBNull(deletedAtOrdinal))
            task.Delete(ParseDateTime(reader.GetString(deletedAtOrdinal)));

        return task;
    }

    private static void AddInsertParameters(SqliteCommand cmd, TaskItem task)
    {
        cmd.Parameters.AddWithValue("@id", task.Id.ToString("D"));
        cmd.Parameters.AddWithValue("@title", task.Title);
        cmd.Parameters.AddWithValue("@description", task.Description);
        cmd.Parameters.AddWithValue("@status", task.Status.ToString());
        cmd.Parameters.AddWithValue("@due_date", task.DueDate.ToString("O"));
        cmd.Parameters.AddWithValue("@user_id", task.UserId.ToString("D"));
        cmd.Parameters.AddWithValue("@created_at", task.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@updated_at", task.UpdatedAt.ToString("O"));
    }

    private static DateTime ParseDateTime(string value) =>
        DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);
}
