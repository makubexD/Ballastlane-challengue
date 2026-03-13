using System.Globalization;
using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using BallastLane.Infrastructure.Common;
using Microsoft.Data.Sqlite;

namespace BallastLane.Infrastructure.Persistence;

public sealed class SqliteUserRepository(IDbConnectionFactory connectionFactory) : IUserRepository
{
    public async Task<Result> SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = (SqliteConnection)await connectionFactory.CreateAsync(cancellationToken);
            using var cmd = new SqliteCommand(UserSql.Insert, connection);
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
        await using var connection = (SqliteConnection)await connectionFactory.CreateAsync(cancellationToken);
        using var cmd = new SqliteCommand(SqliteUserSql.SelectByEmail, connection);
        cmd.Parameters.AddWithValue("@email", email.ToUpperInvariant());
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapToUser(reader) : null;
    }

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = (SqliteConnection)await connectionFactory.CreateAsync(cancellationToken);
        using var cmd = new SqliteCommand(UserSql.SelectById, connection);
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
