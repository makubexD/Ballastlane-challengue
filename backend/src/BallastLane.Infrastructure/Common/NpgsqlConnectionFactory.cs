using System.Data;
using BallastLane.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BallastLane.Infrastructure.Common;

public sealed class NpgsqlConnectionFactory(IOptions<DatabaseSettings> options) : IDbConnectionFactory
{
    private readonly string _connectionString = options.Value.Database;

    public async Task<IDbConnection> CreateAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
