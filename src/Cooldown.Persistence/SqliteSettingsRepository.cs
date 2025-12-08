using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Cooldown.Persistence;

public sealed class SqliteSettingsRepository : ISettingsRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteSettingsRepository>? _logger;

    public SqliteSettingsRepository(string databasePath, ILogger<SqliteSettingsRepository>? logger = null)
    {
        _connectionString = $"Data Source={databasePath}";
        _logger = logger;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = "SELECT ValueJson FROM Settings WHERE Key =  LIMIT 1;";
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("", key);

        var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result as string;
    }

    public async Task SetValueAsync(string key, string valueJson, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = @"INSERT INTO Settings (Key, ValueJson)
                            VALUES (, )
                            ON CONFLICT(Key) DO UPDATE SET ValueJson = excluded.ValueJson;";

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("", key);
        cmd.Parameters.AddWithValue("", valueJson);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        _logger?.LogDebug("Setting saved to SQLite for key {Key}.", key);
    }
}
