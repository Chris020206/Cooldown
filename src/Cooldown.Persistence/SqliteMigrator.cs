using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Cooldown.Persistence;

/// <summary>
/// Minimal forward-only SQLite migrator driven by integer schema versions.
/// Uses PRAGMA user_version for tracking and also keeps the SchemaVersion table in sync.
/// </summary>
public sealed class SqliteMigrator
{
    private readonly ILogger? _logger;
    private readonly IReadOnlyList<ISqliteMigration> _migrations;

    public SqliteMigrator(ILogger? logger = null)
    {
        _logger = logger;
        _migrations = new ISqliteMigration[]
        {
            new CreateLockStateMigration(),
            new CreateSettingsMigration(),
            new CreateSchemaVersionMigration()
        }.OrderBy(m => m.Version).ToArray();
    }

    public async Task<int> MigrateAsync(SqliteConnection connection, CancellationToken cancellationToken = default)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        var currentVersion = await GetCurrentVersionAsync(connection, cancellationToken).ConfigureAwait(false);

        foreach (var migration in _migrations)
        {
            if (migration.Version <= currentVersion)
            {
                continue;
            }

            _logger?.LogInformation("Applying migration v{Version}: {Description}", migration.Version, migration.Description);
            await migration.ApplyAsync(connection, cancellationToken).ConfigureAwait(false);
            await SetVersionAsync(connection, migration.Version, cancellationToken).ConfigureAwait(false);
            currentVersion = migration.Version;
            _logger?.LogInformation("Migration v{Version} complete.", migration.Version);
        }

        return currentVersion;
    }

    private async Task<int> GetCurrentVersionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var pragmaVersion = await ExecuteScalarIntAsync(connection, "PRAGMA user_version;", cancellationToken).ConfigureAwait(false);
        var tableVersion = pragmaVersion;

        if (await TableExistsAsync(connection, "SchemaVersion", cancellationToken).ConfigureAwait(false))
        {
            const string sql = "SELECT Version FROM SchemaVersion WHERE Id = 1 LIMIT 1;";
            tableVersion = await ExecuteScalarIntAsync(connection, sql, cancellationToken).ConfigureAwait(false);
        }

        return Math.Max(pragmaVersion, tableVersion);
    }

    private async Task SetVersionAsync(SqliteConnection connection, int version, CancellationToken cancellationToken)
    {
        await using (var pragmaCmd = connection.CreateCommand())
        {
            pragmaCmd.CommandText = $"PRAGMA user_version = {version};";
            await pragmaCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        if (await TableExistsAsync(connection, "SchemaVersion", cancellationToken).ConfigureAwait(false))
        {
            const string upsertSql = @"INSERT INTO SchemaVersion (Id, Version) VALUES (1, $version)
                                       ON CONFLICT(Id) DO UPDATE SET Version = excluded.Version;";
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = upsertSql;
            cmd.Parameters.AddWithValue("$version", version);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<int> ExecuteScalarIntAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result ?? 0);
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken)
    {
        const string sql = @"SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1;";
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$name", tableName);

        var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result != null;
    }

    private interface ISqliteMigration
    {
        int Version { get; }
        string Description { get; }
        Task ApplyAsync(SqliteConnection connection, CancellationToken cancellationToken);
    }

    private sealed class CreateLockStateMigration : ISqliteMigration
    {
        public int Version => 1;
        public string Description => "Create LockState";

        public async Task ApplyAsync(SqliteConnection connection, CancellationToken cancellationToken)
        {
            const string sql = @"CREATE TABLE IF NOT EXISTS LockState (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                IsActive INTEGER NOT NULL,
                LockType TEXT NOT NULL,
                DurationSeconds INTEGER NOT NULL,
                StartedAtUtc TEXT NOT NULL,
                ExpiresAtUtc TEXT NOT NULL,
                BlockedAppsJson TEXT NOT NULL,
                LastUpdatedUtc TEXT NOT NULL
              );";

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class CreateSettingsMigration : ISqliteMigration
    {
        public int Version => 2;
        public string Description => "Create Settings";

        public async Task ApplyAsync(SqliteConnection connection, CancellationToken cancellationToken)
        {
            const string sql = @"CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                ValueJson TEXT NOT NULL
              );";

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class CreateSchemaVersionMigration : ISqliteMigration
    {
        public int Version => 3;
        public string Description => "Create SchemaVersion metadata";

        public async Task ApplyAsync(SqliteConnection connection, CancellationToken cancellationToken)
        {
            const string sql = @"CREATE TABLE IF NOT EXISTS SchemaVersion (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                Version INTEGER NOT NULL
              );";

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
