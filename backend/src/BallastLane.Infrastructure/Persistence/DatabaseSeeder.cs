using System.Data;
using System.Data.Common;
using BallastLane.Domain.Interfaces;
using Npgsql;
using BallastLane.Domain.ValueObjects;
using BallastLane.Infrastructure.Common;
using BallastLane.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace BallastLane.Infrastructure.Persistence;

public sealed class DatabaseSeeder(
    IDbConnectionFactory connectionFactory,
    IPasswordHasher passwordHasher,
    IOptions<SeedSettings> seedOptions,
    IOptions<DatabaseSettings> dbOptions,
    IDateTimeProvider dateTimeProvider)
{
    private readonly SeedSettings _seed = seedOptions.Value;
    private readonly string _provider = dbOptions.Value.Provider;

    private const string CountUsersQuery = "SELECT COUNT(*) FROM users;";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await connectionFactory.CreateAsync(cancellationToken);

        var userCount = await CountUsersAsync(connection, cancellationToken);
        if (userCount > 0)
            return;

        var userId = Guid.NewGuid();
        var now = dateTimeProvider.UtcNow;
        var passwordHash = passwordHasher.Hash(_seed.DemoUserPassword);

        await InsertDemoUserAsync(connection, userId, passwordHash, now, cancellationToken);
        await InsertDemoTasksAsync(connection, userId, now, cancellationToken);
    }

    private static async Task<long> CountUsersAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = CountUsersQuery;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result ?? 0L);
    }

    private async Task InsertDemoUserAsync(
        DbConnection connection,
        Guid userId,
        string passwordHash,
        DateTime now,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = UserSql.Insert;
        AddParam(command, "@id", FormatGuid(userId));
        AddParam(command, "@email", _seed.DemoUserEmail);
        AddParam(command, "@password_hash", passwordHash);
        AddParam(command, "@created_at", FormatDateTime(now));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task InsertDemoTasksAsync(
        DbConnection connection,
        Guid userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var tasks = new[]
        {
            (Guid.NewGuid(), "Set up project repository", "Initialize the Git repo, configure .gitignore, and set up Docker Compose for the development database.", TaskItemStatus.Todo.ToString(), now.AddDays(7)),
            (Guid.NewGuid(), "Implement authentication API", "Build the register and login endpoints with JWT token generation and BCrypt password hashing.", TaskItemStatus.InProgress.ToString(), now.AddDays(3)),
            (Guid.NewGuid(), "Write unit tests for TaskService", "Achieve 100% coverage on TaskService using xUnit and Moq, following Red-Green-Refactor.", TaskItemStatus.Done.ToString(), now.AddDays(1))
        };

        foreach (var (id, title, description, status, dueDate) in tasks)
        {
            using var command = connection.CreateCommand();
            command.CommandText = TaskSql.Insert;
            AddParam(command, "@id", FormatGuid(id));
            AddParam(command, "@title", title);
            AddParam(command, "@description", description);
            AddParam(command, "@status", status);
            AddParam(command, "@due_date", FormatDateTime(dueDate));
            AddParam(command, "@user_id", FormatGuid(userId));
            AddParam(command, "@created_at", FormatDateTime(now));
            AddParam(command, "@updated_at", FormatDateTime(now));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static void AddParam(IDbCommand command, string name, object value)
    {
        if (command is NpgsqlCommand npgsql)
        {
            npgsql.Parameters.AddWithValue(name, value);
            return;
        }
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }

    /// <summary>
    /// SQLite stores GUIDs as TEXT (UUID dash format); PostgreSQL has a native UUID type.
    /// Npgsql auto-converts <see cref="Guid"/> to UUID. For provider-agnostic code, always pass strings.
    /// </summary>
    private object FormatGuid(Guid value) =>
        _provider == "SQLite" ? (object)value.ToString("D") : value;

    /// <summary>
    /// SQLite stores DateTimes as ISO 8601 TEXT; PostgreSQL has native TIMESTAMPTZ.
    /// Npgsql maps <see cref="DateTime"/> → timestamptz directly; pass the object to avoid text-type mismatch.
    /// </summary>
    private object FormatDateTime(DateTime value) =>
        _provider == "SQLite" ? (object)value.ToString("O") : value;
}
