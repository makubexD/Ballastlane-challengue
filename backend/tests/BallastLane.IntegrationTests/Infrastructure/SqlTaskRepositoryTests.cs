using System.Data;
using BallastLane.Domain.Entities;
using BallastLane.Domain.ValueObjects;
using BallastLane.Infrastructure.Common;
using BallastLane.Infrastructure.Persistence;
using BallastLane.Tests.TestData;
using Npgsql;

namespace BallastLane.IntegrationTests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class SqlTaskRepositoryTests : IAsyncLifetime
{
    private const string TestUserEmail = "testuser@example.com";
    private const string OtherUserEmail = "otheruser@example.com";
    private const string TestPasswordHash = "$2a$12$placeholder";
    private const string TaskTitleUserA1 = "User A Task 1";
    private const string TaskTitleUserA2 = "User A Task 2";
    private const string TaskTitleUserB1 = "User B Task 1";
    private const string UpdatedTitle = "Updated Title";

    private const string InsertTestUserSql =
        "INSERT INTO users (id, email, password_hash, created_at) VALUES (@id, @email, @password_hash, @created_at);";

    private const string DeleteTasksSql = "DELETE FROM tasks WHERE user_id = @user_id OR user_id = @other_user_id;";
    private const string DeleteUserSql = "DELETE FROM users WHERE id = @id OR id = @other_id;";

    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION") ?? TestConstants.IntegrationDbConnectionString;

    private readonly Guid _testUserId = TestConstants.ValidUserId;
    private readonly Guid _otherUserId = TestConstants.OtherUserId;

    private DirectConnectionFactory _connectionFactory = null!;
    private SqlTaskRepository _sut = null!;

    public async Task InitializeAsync()
    {
        _connectionFactory = new DirectConnectionFactory(_connectionString);
        var migrator = new DatabaseMigrator(_connectionFactory);
        await migrator.MigrateAsync();

        await InsertTestUserAsync(_testUserId, TestUserEmail);
        await InsertTestUserAsync(_otherUserId, OtherUserEmail);

        _sut = new SqlTaskRepository(_connectionFactory, new SystemDateTimeProvider());
    }

    public async Task DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var deleteTasksCmd = new NpgsqlCommand(DeleteTasksSql, connection);
        deleteTasksCmd.Parameters.AddWithValue("@user_id", _testUserId);
        deleteTasksCmd.Parameters.AddWithValue("@other_user_id", _otherUserId);
        await deleteTasksCmd.ExecuteNonQueryAsync();

        await using var deleteUserCmd = new NpgsqlCommand(DeleteUserSql, connection);
        deleteUserCmd.Parameters.AddWithValue("@id", _testUserId);
        deleteUserCmd.Parameters.AddWithValue("@other_id", _otherUserId);
        await deleteUserCmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistTask_WhenTaskIsValid()
    {
        var task = TestDataBuilder.ValidTask(userId: _testUserId);

        await _sut.SaveAsync(task);

        var persisted = await _sut.GetByIdAsync(task.Id);
        Assert.NotNull(persisted);
        Assert.Equal(task.Id, persisted.Id);
        Assert.Equal(task.Title, persisted.Title);
        Assert.Equal(task.Description, persisted.Description);
        Assert.Equal(task.Status, persisted.Status);
        Assert.Equal(task.DueDate, persisted.DueDate, TimeSpan.FromSeconds(1));
        Assert.Equal(task.UserId, persisted.UserId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTask_WhenTaskExists()
    {
        var task = TestDataBuilder.ValidTask(id: Guid.NewGuid(), userId: _testUserId);
        await _sut.SaveAsync(task);

        var result = await _sut.GetByIdAsync(task.Id);

        Assert.NotNull(result);
        Assert.Equal(task.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenTaskDoesNotExist()
    {
        var result = await _sut.GetByIdAsync(TestConstants.UnknownTaskId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllByUserIdAsync_ShouldReturnOnlyTasksForSpecifiedUser()
    {
        var taskA1 = TestDataBuilder.ValidTask(id: Guid.NewGuid(), userId: _testUserId, title: TaskTitleUserA1);
        var taskA2 = TestDataBuilder.ValidTask(id: Guid.NewGuid(), userId: _testUserId, title: TaskTitleUserA2);
        var taskB1 = TestDataBuilder.ValidTask(id: Guid.NewGuid(), userId: _otherUserId, title: TaskTitleUserB1);
        await _sut.SaveAsync(taskA1);
        await _sut.SaveAsync(taskA2);
        await _sut.SaveAsync(taskB1);

        var results = await _sut.GetAllByUserIdAsync(_testUserId);

        Assert.Equal(2, results.Count);
        Assert.All(results, t => Assert.Equal(_testUserId, t.UserId));
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyFields_WhenTaskExists()
    {
        var task = TestDataBuilder.ValidTask(id: Guid.NewGuid(), userId: _testUserId);
        await _sut.SaveAsync(task);

        var updated = TaskItem.Create(
            task.Id,
            UpdatedTitle,
            task.Description,
            TaskItemStatus.InProgress,
            task.DueDate,
            task.UserId);
        await _sut.UpdateAsync(updated);

        var persisted = await _sut.GetByIdAsync(task.Id);
        Assert.NotNull(persisted);
        Assert.Equal(UpdatedTitle, persisted.Title);
        Assert.Equal(TaskItemStatus.InProgress, persisted.Status);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTask_WhenTaskExists()
    {
        var task = TestDataBuilder.ValidTask(id: Guid.NewGuid(), userId: _testUserId);
        await _sut.SaveAsync(task);

        await _sut.DeleteAsync(task.Id);

        var result = await _sut.GetByIdAsync(task.Id);
        Assert.Null(result);
    }

    private async Task InsertTestUserAsync(Guid userId, string email)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(InsertTestUserSql, connection);
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@password_hash", TestPasswordHash);
        cmd.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();
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
