namespace BallastLane.Infrastructure.Persistence;

internal static class SqliteUserSql
{
    internal const string SelectByEmail = """
        SELECT id, email, password_hash, created_at
        FROM users WHERE UPPER(email) = @email;
        """;
}
