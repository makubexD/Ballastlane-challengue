using System.Globalization;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using Microsoft.Data.Sqlite;

namespace BallastLane.Infrastructure.Persistence;

/// <summary>
/// IUserRepository implementation that participates in a shared SqliteTransaction.
/// GUIDs stored as TEXT (UUID dash format); DateTimes stored as ISO 8601 TEXT.
/// </summary>
internal sealed class SqliteTransactionalUserRepository(
    SqliteConnection connection,
    SqliteTransaction transaction) : IUserRepository
{
    public async Task<Result> SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            using var cmd = new SqliteCommand(UserSql.Insert, connection, transaction);
            cmd.Parameters.AddWithValue("@id", user.Id.ToString("D"));
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@password_hash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@created_at", user.CreatedAt.ToString("O"));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            return Result.Ok();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
        {
            return Result.Fail("An account with this email already exists.", ResultErrorType.Conflict);
        }
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var cmd = new SqliteCommand(SqliteUserSql.SelectByEmail, connection, transaction);
        cmd.Parameters.AddWithValue("@email", email.ToUpperInvariant());
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapToUser(reader) : null;
    }

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var cmd = new SqliteCommand(UserSql.SelectById, connection, transaction);
        cmd.Parameters.AddWithValue("@id", id.ToString("D"));
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapToUser(reader) : null;
    }

    private static User MapToUser(SqliteDataReader reader) => User.Create(
        Guid.Parse(reader.GetString(reader.GetOrdinal(UserSql.ColId))),
        reader.GetString(reader.GetOrdinal(UserSql.ColEmail)),
        reader.GetString(reader.GetOrdinal(UserSql.ColPasswordHash)),
        DateTime.Parse(reader.GetString(reader.GetOrdinal(UserSql.ColCreatedAt)), null,
            DateTimeStyles.RoundtripKind));
}
