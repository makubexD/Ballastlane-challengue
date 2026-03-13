using System.Data;
using System.Data.Common;
using System.Reflection;
using BallastLane.Infrastructure.Common;
using BallastLane.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace BallastLane.Infrastructure.Persistence;

public sealed class DatabaseMigrator(IDbConnectionFactory connectionFactory, IOptions<DatabaseSettings> options)
{
    private readonly string _provider = options.Value.Provider;

    private string CreateMigrationsTableSql => _provider == "SQLite"
        ? """
          CREATE TABLE IF NOT EXISTS _schema_migrations (
              version TEXT PRIMARY KEY,
              applied_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
          );
          """
        : """
          CREATE TABLE IF NOT EXISTS _schema_migrations (
              version VARCHAR(255) PRIMARY KEY,
              applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
          );
          """;

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await connectionFactory.CreateAsync(cancellationToken);

        await ExecuteAsync(connection, CreateMigrationsTableSql, cancellationToken);

        var applied = await GetAppliedMigrationsAsync(connection, cancellationToken);
        var pending = GetPendingMigrations(applied);

        foreach (var (version, sql) in pending)
        {
            await ExecuteAsync(connection, sql, cancellationToken);
            await RecordMigrationAsync(connection, version, cancellationToken);
        }
    }

    private List<(string Version, string Sql)> GetPendingMigrations(HashSet<string> applied)
    {
        var assembly = typeof(DatabaseMigrator).Assembly;
        var prefix = $"BallastLane.Infrastructure.Persistence.Migrations.{_provider}.";

        return assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix) && n.EndsWith(".sql"))
            .OrderBy(n => n)
            .Select(n => (Version: ExtractVersion(n, prefix), ResourceName: n))
            .Where(x => !applied.Contains(x.Version))
            .Select(x => (x.Version, Sql: ReadResource(assembly, x.ResourceName)))
            .ToList();
    }

    private static string ExtractVersion(string resourceName, string prefix)
    {
        var fileName = resourceName[prefix.Length..];
        return fileName[..fileName.IndexOf('_')];
    }

    private static string ReadResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async Task<HashSet<string>> GetAppliedMigrationsAsync(
        DbConnection connection, CancellationToken cancellationToken)
    {
        var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT version FROM _schema_migrations;";
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            applied.Add(reader.GetString(0));
        return applied;
    }

    private static async Task RecordMigrationAsync(
        DbConnection connection, string version, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO _schema_migrations (version) VALUES (@version);";
        var param = command.CreateParameter();
        param.ParameterName = "@version";
        param.Value = version;
        command.Parameters.Add(param);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteAsync(
        DbConnection connection, string sql, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
