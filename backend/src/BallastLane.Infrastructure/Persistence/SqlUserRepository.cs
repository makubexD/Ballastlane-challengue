using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using BallastLane.Infrastructure.Common;
using Npgsql;

namespace BallastLane.Infrastructure.Persistence;

public sealed class SqlUserRepository(IDbConnectionFactory connectionFactory) : IUserRepository
{
    public async Task<Result> SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
            await using var command = new NpgsqlCommand(UserSql.Insert, connection);
            command.Parameters.AddWithValue("@id", user.Id);
            command.Parameters.AddWithValue("@email", user.Email);
            command.Parameters.AddWithValue("@password_hash", user.PasswordHash);
            command.Parameters.AddWithValue("@created_at", user.CreatedAt);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return Result.Ok();
        }
        catch (PostgresException ex) when (ex.SqlState == UserSql.DuplicateKeyViolation)
        {
            return Result.Fail("An account with this email already exists.", ResultErrorType.Conflict);
        }
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(UserSql.SelectByEmail, connection);
        command.Parameters.AddWithValue("@email", email);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? UserSql.MapToUser(reader) : null;
    }

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(UserSql.SelectById, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? UserSql.MapToUser(reader) : null;
    }
}
