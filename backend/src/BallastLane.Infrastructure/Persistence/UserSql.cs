using BallastLane.Domain.Entities;
using Npgsql;

namespace BallastLane.Infrastructure.Persistence;

internal static class UserSql
{
    internal const string Insert = """
        INSERT INTO users (id, email, password_hash, created_at)
        VALUES (@id, @email, @password_hash, @created_at);
        """;

    internal const string SelectByEmail = """
        SELECT id, email, password_hash, created_at
        FROM users WHERE LOWER(email) = LOWER(@email);
        """;

    internal const string SelectById = """
        SELECT id, email, password_hash, created_at
        FROM users WHERE id = @id;
        """;

    internal const string DuplicateKeyViolation = "23505";

    internal const string ColId = "id";
    internal const string ColEmail = "email";
    internal const string ColPasswordHash = "password_hash";
    internal const string ColCreatedAt = "created_at";

    internal static User MapToUser(NpgsqlDataReader reader) => User.Create(
        reader.GetGuid(reader.GetOrdinal(ColId)),
        reader.GetString(reader.GetOrdinal(ColEmail)),
        reader.GetString(reader.GetOrdinal(ColPasswordHash)));
}
