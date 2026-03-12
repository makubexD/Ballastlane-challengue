using BallastLane.Domain.Interfaces;
using Npgsql;

namespace BallastLane.Infrastructure.Persistence;

public sealed class DatabaseSeeder(string connectionString, IPasswordHasher passwordHasher)
{
    private const string DemoUserEmail = "demo@ballastlane.com";
    private const string DemoUserPassword = "Demo@1234";

    private const string CountUsersQuery = "SELECT COUNT(*) FROM users;";

    private const string InsertUserQuery = """
        INSERT INTO users (id, email, password_hash, created_at)
        VALUES (@id, @email, @password_hash, @created_at);
        """;

    private const string InsertTaskQuery = """
        INSERT INTO tasks (id, title, description, status, due_date, user_id, created_at, updated_at)
        VALUES (@id, @title, @description, @status, @due_date, @user_id, @created_at, @updated_at);
        """;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var userCount = await CountUsersAsync(connection, cancellationToken);
        if (userCount > 0)
            return;

        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var passwordHash = passwordHasher.Hash(DemoUserPassword);

        await InsertDemoUserAsync(connection, userId, passwordHash, now, cancellationToken);
        await InsertDemoTasksAsync(connection, userId, now, cancellationToken);
    }

    private static async Task<long> CountUsersAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(CountUsersQuery, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return (long)(result ?? 0L);
    }

    private static async Task InsertDemoUserAsync(
        NpgsqlConnection connection,
        Guid userId,
        string passwordHash,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(InsertUserQuery, connection);
        command.Parameters.AddWithValue("@id", userId);
        command.Parameters.AddWithValue("@email", DemoUserEmail);
        command.Parameters.AddWithValue("@password_hash", passwordHash);
        command.Parameters.AddWithValue("@created_at", now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertDemoTasksAsync(
        NpgsqlConnection connection,
        Guid userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var tasks = new[]
        {
            (Guid.NewGuid(), "Set up project repository", "Initialize the Git repo, configure .gitignore, and set up Docker Compose for the development database.", "Todo", now.AddDays(7)),
            (Guid.NewGuid(), "Implement authentication API", "Build the register and login endpoints with JWT token generation and BCrypt password hashing.", "InProgress", now.AddDays(3)),
            (Guid.NewGuid(), "Write unit tests for TaskService", "Achieve 100% coverage on TaskService using xUnit and Moq, following Red-Green-Refactor.", "Done", now.AddDays(1))
        };

        foreach (var (id, title, description, status, dueDate) in tasks)
        {
            await using var command = new NpgsqlCommand(InsertTaskQuery, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@description", description);
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@due_date", dueDate);
            command.Parameters.AddWithValue("@user_id", userId);
            command.Parameters.AddWithValue("@created_at", now);
            command.Parameters.AddWithValue("@updated_at", now);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
