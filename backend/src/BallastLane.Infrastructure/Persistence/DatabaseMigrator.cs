using Npgsql;

namespace BallastLane.Infrastructure.Persistence;

public sealed class DatabaseMigrator(string connectionString)
{
    private const string CreateUsersTable = """
        CREATE TABLE IF NOT EXISTS users (
            id UUID PRIMARY KEY,
            email VARCHAR(200) UNIQUE NOT NULL,
            password_hash TEXT NOT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """;

    private const string CreateTasksTable = """
        CREATE TABLE IF NOT EXISTS tasks (
            id UUID PRIMARY KEY,
            title VARCHAR(200) NOT NULL,
            description TEXT NOT NULL,
            status VARCHAR(20) NOT NULL CHECK (status IN ('Todo', 'InProgress', 'Done')),
            due_date TIMESTAMPTZ NOT NULL,
            user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """;

    private const string CreateTasksUserIdIndex =
        "CREATE INDEX IF NOT EXISTS idx_tasks_user_id ON tasks(user_id);";

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await ExecuteAsync(connection, CreateUsersTable, cancellationToken);
        await ExecuteAsync(connection, CreateTasksTable, cancellationToken);
        await ExecuteAsync(connection, CreateTasksUserIdIndex, cancellationToken);
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
