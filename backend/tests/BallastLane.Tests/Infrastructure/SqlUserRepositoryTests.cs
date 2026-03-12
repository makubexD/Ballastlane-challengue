using System.Data;
using BallastLane.Domain.Entities;
using BallastLane.Infrastructure.Common;
using BallastLane.Infrastructure.Persistence;
using BallastLane.Tests.TestData;
using Npgsql;

namespace BallastLane.Tests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class SqlUserRepositoryTests : IAsyncLifetime
{
    private const string FindByIdEmail = "findbyid@example.com";

    private const string DeleteUsersSql =
        "DELETE FROM users WHERE id = @id OR id = @duplicate_id OR id = @find_id;";

    private const string TestEmail = "sqluser-test@example.com";
    private const string TestEmailMixedCase = "SqlUser-Test@Example.Com";
    private const string UnknownEmail = "nobody@nowhere.invalid";
    private const string TestPasswordHash = "$2a$12$testpasswordhashvalue";

    private static readonly Guid TestUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DuplicateUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid FindUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid UnknownUserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION") ?? TestConstants.IntegrationDbConnectionString;

    private DirectConnectionFactory _connectionFactory = null!;
    private SqlUserRepository _sut = null!;

    public async Task InitializeAsync()
    {
        _connectionFactory = new DirectConnectionFactory(_connectionString);
        var migrator = new DatabaseMigrator(_connectionString);
        await migrator.MigrateAsync();
        _sut = new SqlUserRepository(_connectionFactory);
    }

    public async Task DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(DeleteUsersSql, connection);
        cmd.Parameters.AddWithValue("@id", TestUserId);
        cmd.Parameters.AddWithValue("@duplicate_id", DuplicateUserId);
        cmd.Parameters.AddWithValue("@find_id", FindUserId);
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistUser_WhenEmailIsUnique()
    {
        var user = User.Create(TestUserId, TestEmail, TestPasswordHash);

        var result = await _sut.SaveAsync(user);

        Assert.True(result.IsSuccess);
        var persisted = await _sut.FindByIdAsync(TestUserId);
        Assert.NotNull(persisted);
        Assert.Equal(TestEmail.ToLowerInvariant(), persisted.Email);
        Assert.Equal(TestPasswordHash, persisted.PasswordHash);
    }

    [Fact]
    public async Task SaveAsync_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        var firstUser = User.Create(TestUserId, TestEmail, TestPasswordHash);
        var duplicateUser = User.Create(DuplicateUserId, TestEmail, TestPasswordHash);
        await _sut.SaveAsync(firstUser);

        var result = await _sut.SaveAsync(duplicateUser);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task FindByEmailAsync_ShouldReturnUser_WhenEmailExistsCaseInsensitive()
    {
        var user = User.Create(TestUserId, TestEmail, TestPasswordHash);
        await _sut.SaveAsync(user);

        var found = await _sut.FindByEmailAsync(TestEmailMixedCase);

        Assert.NotNull(found);
        Assert.Equal(TestEmail.ToLowerInvariant(), found.Email);
    }

    [Fact]
    public async Task FindByEmailAsync_ShouldReturnNull_WhenEmailNotFound()
    {
        var result = await _sut.FindByEmailAsync(UnknownEmail);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByIdAsync_ShouldReturnUser_WhenIdExists()
    {
        var user = User.Create(FindUserId, FindByIdEmail, TestPasswordHash);
        await _sut.SaveAsync(user);

        var found = await _sut.FindByIdAsync(FindUserId);

        Assert.NotNull(found);
        Assert.Equal(FindUserId, found.Id);
    }

    private sealed class DirectConnectionFactory(string connectionString) : IDbConnectionFactory
    {
        public async Task<IDbConnection> CreateAsync(CancellationToken cancellationToken = default)
        {
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
