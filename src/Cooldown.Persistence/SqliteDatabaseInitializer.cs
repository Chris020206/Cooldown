using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Cooldown.Persistence;

public sealed class SqliteDatabaseInitializer
{
    private readonly ILogger<SqliteDatabaseInitializer>? _logger;
    private readonly string _dbPath;

    public SqliteDatabaseInitializer(string dbPath, ILogger<SqliteDatabaseInitializer>? logger = null)
    {
        _dbPath = dbPath;
        _logger = logger;
    }

    public async Task InitDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _logger?.LogInformation("Initializing SQLite persistence at {Path}.", _dbPath);

        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var commands = new[]
        {
            @"CREATE TABLE IF NOT EXISTS SchemaVersion (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                Version INTEGER NOT NULL
              );",
            @"INSERT OR IGNORE INTO SchemaVersion (Id, Version) VALUES (1, 1);",
            @"CREATE TABLE IF NOT EXISTS LockState (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                IsActive INTEGER NOT NULL,
                LockType TEXT NOT NULL,
                DurationSeconds INTEGER NOT NULL,
                StartedAtUtc TEXT NOT NULL,
                ExpiresAtUtc TEXT NOT NULL,
                BlockedAppsJson TEXT NOT NULL,
                LastUpdatedUtc TEXT NOT NULL
              );",
            @"CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                ValueJson TEXT NOT NULL
              );"
        };

        foreach (var sql in commands)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        _logger?.LogInformation("SQLite schema ensured (LockState, Settings, SchemaVersion).");
    }
}
