using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BallastLane.Infrastructure.Common;

public sealed class NpgsqlConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private const string ConnectionStringKey = "Database";

    public async Task<IDbConnection> CreateAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringKey)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringKey}' is not configured.");

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
