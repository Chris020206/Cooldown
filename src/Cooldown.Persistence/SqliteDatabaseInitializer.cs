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

        var migrator = new SqliteMigrator(_logger);
        var finalVersion = await migrator.MigrateAsync(connection, cancellationToken).ConfigureAwait(false);

        _logger?.LogInformation("SQLite schema up to date (version {Version}).", finalVersion);
    }
}
