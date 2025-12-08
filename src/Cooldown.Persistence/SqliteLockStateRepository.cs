using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Cooldown.Persistence;

/// <summary>
/// SQLite implementation that stores a single row representing the latest lock state.
/// Approach: DELETE all rows before inserting the new state so only one row remains.
/// </summary>
public sealed class SqliteLockStateRepository : ILockStateRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteLockStateRepository>? _logger;

    public SqliteLockStateRepository(string databasePath, ILogger<SqliteLockStateRepository>? logger = null)
    {
        _connectionString = $"Data Source={databasePath}";
        _logger = logger;
    }

    public async Task<LockStateRecord?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string cleanupSql = @"DELETE FROM LockState WHERE Id NOT IN (SELECT Id FROM LockState ORDER BY Id DESC LIMIT 1);";
        const string sql = @"SELECT IsActive, LockType, DurationSeconds, StartedAtUtc, ExpiresAtUtc, BlockedAppsJson, LastUpdatedUtc
                              FROM LockState
                              ORDER BY Id DESC
                              LIMIT 1;";

        // Best effort cleanup if multiple rows exist.
        await using (var cleanupCmd = connection.CreateCommand())
        {
            cleanupCmd.CommandText = cleanupSql;
            await cleanupCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        try
        {
            var blockedAppsJson = reader.GetString(5);
            var blockedApps = string.IsNullOrWhiteSpace(blockedAppsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(blockedAppsJson) ?? new List<string>();

            return new LockStateRecord
            {
                IsActive = reader.GetInt32(0) == 1,
                LockType = reader.GetString(1),
                DurationSeconds = reader.GetInt32(2),
                StartedAtUtc = DateTimeOffset.Parse(reader.GetString(3)),
                ExpiresAtUtc = DateTimeOffset.Parse(reader.GetString(4)),
                BlockedApps = blockedApps,
                LastUpdatedUtc = DateTimeOffset.Parse(reader.GetString(6))
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to map lock state row from SQLite.");
            return null;
        }
    }

    public async Task SaveAsync(LockStateRecord state, CancellationToken cancellationToken = default)
    {
        var blockedAppsJson = JsonSerializer.Serialize(state.BlockedApps ?? Array.Empty<string>());

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Simplest single-row enforcement: delete all then insert.
        const string deleteSql = "DELETE FROM LockState;";
        const string insertSql = @"INSERT INTO LockState (IsActive, LockType, DurationSeconds, StartedAtUtc, ExpiresAtUtc, BlockedAppsJson, LastUpdatedUtc)
                                   VALUES ($isActive, $lockType, $durationSeconds, $startedAtUtc, $expiresAtUtc, $blockedAppsJson, $lastUpdatedUtc);";

        await using var deleteCmd = connection.CreateCommand();
        deleteCmd.CommandText = deleteSql;
        await deleteCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        await using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = insertSql;
        insertCmd.Parameters.AddWithValue("$isActive", state.IsActive ? 1 : 0);
        insertCmd.Parameters.AddWithValue("$lockType", state.LockType);
        insertCmd.Parameters.AddWithValue("$durationSeconds", state.DurationSeconds);
        insertCmd.Parameters.AddWithValue("$startedAtUtc", state.StartedAtUtc.ToString("o"));
        insertCmd.Parameters.AddWithValue("$expiresAtUtc", state.ExpiresAtUtc.ToString("o"));
        insertCmd.Parameters.AddWithValue("$blockedAppsJson", blockedAppsJson);
        insertCmd.Parameters.AddWithValue("$lastUpdatedUtc", state.LastUpdatedUtc.ToString("o"));

        await insertCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        _logger?.LogInformation("Lock state saved to SQLite (isActive={IsActive}, type={Type}, durationSeconds={Duration}, blockedApps={BlockedApps}).",
            state.IsActive,
            state.LockType,
            state.DurationSeconds,
            state.BlockedApps?.Count ?? 0);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = "DELETE FROM LockState;";
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        _logger?.LogInformation("Lock state cleared in SQLite.");
    }
}
