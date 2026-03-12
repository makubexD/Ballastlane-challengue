using BallastLane.Domain.Common;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using BallastLane.Infrastructure.Common;
using Npgsql;

namespace BallastLane.Infrastructure.Persistence;

public sealed class SqlUserRepository(IDbConnectionFactory connectionFactory) : IUserRepository
{
    private const string InsertSql = """
        INSERT INTO users (id, email, password_hash, created_at)
        VALUES (@id, @email, @password_hash, @created_at);
        """;

    private const string SelectByEmailSql = """
        SELECT id, email, password_hash, created_at
        FROM users WHERE LOWER(email) = LOWER(@email);
        """;

    private const string SelectByIdSql = """
        SELECT id, email, password_hash, created_at
        FROM users WHERE id = @id;
        """;

    private const string DuplicateKeyViolation = "23505";

    private const string ColId = "id";
    private const string ColEmail = "email";
    private const string ColPasswordHash = "password_hash";
    private const string ColCreatedAt = "created_at";

    public async Task<Result> SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
            await using var command = new NpgsqlCommand(InsertSql, connection);
            command.Parameters.AddWithValue("@id", user.Id);
            command.Parameters.AddWithValue("@email", user.Email);
            command.Parameters.AddWithValue("@password_hash", user.PasswordHash);
            command.Parameters.AddWithValue("@created_at", user.CreatedAt);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return Result.Ok();
        }
        catch (PostgresException ex) when (ex.SqlState == DuplicateKeyViolation)
        {
            return Result.Fail("An account with this email already exists.");
        }
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(SelectByEmailSql, connection);
        command.Parameters.AddWithValue("@email", email);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapToUser(reader) : null;
    }

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = (NpgsqlConnection)await connectionFactory.CreateAsync(cancellationToken);
        await using var command = new NpgsqlCommand(SelectByIdSql, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapToUser(reader) : null;
    }

    private static User MapToUser(NpgsqlDataReader reader) => User.Create(
        reader.GetGuid(reader.GetOrdinal(ColId)),
        reader.GetString(reader.GetOrdinal(ColEmail)),
        reader.GetString(reader.GetOrdinal(ColPasswordHash)));
}
