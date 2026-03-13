using System.Data;
using BallastLane.Infrastructure.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace BallastLane.Infrastructure.Common;

public sealed class SqliteConnectionFactory(IOptions<DatabaseSettings> options) : IDbConnectionFactory
{
    private readonly string _connectionString = options.Value.Database;

    public async Task<IDbConnection> CreateAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        await pragma.ExecuteNonQueryAsync(cancellationToken);
        pragma.Dispose();
        return connection;
    }
}
