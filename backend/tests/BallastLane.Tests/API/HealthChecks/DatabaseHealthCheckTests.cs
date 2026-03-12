using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using BallastLane.API.HealthChecks;
using BallastLane.Infrastructure.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace BallastLane.Tests.API.HealthChecks;

public sealed class DatabaseHealthCheckTests
{
    private static (DatabaseHealthCheck sut, Mock<IDbConnectionFactory> factory) BuildSut(
        bool dbThrows = false)
    {
        var factory = new Mock<IDbConnectionFactory>();
        factory
            .Setup(f => f.CreateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeDbConnection(dbThrows));
        return (new DatabaseHealthCheck(factory.Object), factory);
    }

    private static HealthCheckContext BuildContext(IHealthCheck check) =>
        new() { Registration = new HealthCheckRegistration("database", check, null, null) };

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenDatabaseIsReachable()
    {
        var (sut, _) = BuildSut(dbThrows: false);

        var result = await sut.CheckHealthAsync(BuildContext(sut), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Database is reachable.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenDatabaseThrows()
    {
        var (sut, _) = BuildSut(dbThrows: true);

        var result = await sut.CheckHealthAsync(BuildContext(sut), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Database is unreachable.", result.Description);
        Assert.NotNull(result.Exception);
    }

    // Minimal in-process fakes — avoids Moq protected-member complexity for DbConnection/DbCommand.
    // Explicit backing fields are used for ConnectionString and CommandText to satisfy the split-nullable
    // contract (getter: string, setter: string?) that .NET emits for those abstract members.
    private sealed class FakeDbConnection(bool throws) : DbConnection
    {
        private string _connectionString = string.Empty;

        [AllowNull]
        public override string ConnectionString
        {
            get => _connectionString;
            set => _connectionString = value ?? string.Empty;
        }

        public override string Database => string.Empty;
        public override string DataSource => string.Empty;
        public override string ServerVersion => string.Empty;
        public override ConnectionState State => ConnectionState.Open;

        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            throw new NotSupportedException();

        protected override DbCommand CreateDbCommand() => new FakeDbCommand(throws);
    }

    private sealed class FakeDbCommand(bool throws) : DbCommand
    {
        private string _commandText = string.Empty;

        [AllowNull]
        public override string CommandText
        {
            get => _commandText;
            set => _commandText = value ?? string.Empty;
        }

        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } =
            new FakeParameterCollection();
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }
        public override int ExecuteNonQuery() => 0;
        public override object? ExecuteScalar() => throws
            ? throw new InvalidOperationException("connection lost")
            : (object)1;
        public override void Prepare() { }

        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) =>
            throws
                ? Task.FromException<object?>(new InvalidOperationException("connection lost"))
                : Task.FromResult<object?>(1);

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
            throw new NotSupportedException();

        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();
    }

    private sealed class FakeParameterCollection : DbParameterCollection
    {
        private readonly List<object> _items = [];
        public override int Count => _items.Count;
        public override object SyncRoot => _items;
        public override int Add(object? value) { _items.Add(value!); return _items.Count - 1; }
        public override void AddRange(Array values) { foreach (var v in values) _items.Add(v!); }
        public override void Clear() => _items.Clear();
        public override bool Contains(object? value) => _items.Contains(value!);
        public override bool Contains(string value) => false;
        public override void CopyTo(Array array, int index) =>
            ((System.Collections.ICollection)_items).CopyTo(array, index);
        public override System.Collections.IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override int IndexOf(object value) => _items.IndexOf(value);
        public override int IndexOf(string parameterName) => -1;
        public override void Insert(int index, object value) => _items.Insert(index, value);
        public override void Remove(object value) => _items.Remove(value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) { }
        protected override DbParameter GetParameter(int index) => (DbParameter)_items[index];
        protected override DbParameter GetParameter(string parameterName) => throw new NotSupportedException();
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) { }
    }
}
