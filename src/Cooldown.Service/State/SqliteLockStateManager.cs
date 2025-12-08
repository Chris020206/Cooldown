using Cooldown.Persistence;
using Microsoft.Extensions.Logging;

namespace Cooldown.Service.State;

/// <summary>
/// SQLite-backed implementation of ILockStateManager.
/// Keeps an in-memory cached lock state and writes through to the LockState table.
/// Startup rehydration is intentionally deferred to P2.3-03.
/// </summary>
public sealed class SqliteLockStateManager : ILockStateManager
{
    private readonly ILockStateRepository _repository;
    private readonly ILogger<SqliteLockStateManager> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private LockState? _current;

    public SqliteLockStateManager(ILockStateRepository repository, ILogger<SqliteLockStateManager> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<LockState?> GetCurrentLockAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_current != null)
            {
                _logger.LogDebug("SqliteLockStateManager: returning cached lock (type={Type}, expiresAt={ExpiresAt}).", _current.Type, _current.ExpiresAt);
            }
            else
            {
                _logger.LogDebug("SqliteLockStateManager: no cached lock (rehydration deferred to P2.3-03).");
            }

            return _current;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LockState> StartLockAsync(LockParameters parameters, CancellationToken cancellationToken = default)
    {
        if (parameters.Duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters.Duration), "Duration must be positive.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var newState = new LockState
            {
                Type = parameters.Type,
                StartedAt = now,
                ExpiresAt = now.Add(parameters.Duration),
                Duration = parameters.Duration,
                BlockedApps = parameters.BlockedApps?.ToArray() ?? Array.Empty<string>()
            };

            _current = newState;

            await _repository.SaveAsync(ToRecord(newState, now), cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("SqliteLockStateManager: started lock type={Type} duration={Duration} blockedApps={BlockedApps} (persisted).",
                newState.Type,
                newState.Duration,
                newState.BlockedApps.Count);

            return newState;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task CancelLockAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_current == null)
            {
                _logger.LogInformation("SqliteLockStateManager: cancel requested but no active lock.");
                return;
            }

            _current = null;
            await _repository.ClearAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("SqliteLockStateManager: canceled active lock (cleared SQLite).");
        }
        finally
        {
            _gate.Release();
        }
    }

    private static LockStateRecord ToRecord(LockState state, DateTimeOffset asOfUtc)
    {
        return new LockStateRecord
        {
            IsActive = true,
            LockType = state.Type.ToString(),
            DurationSeconds = (int)state.Duration.TotalSeconds,
            StartedAtUtc = state.StartedAt,
            ExpiresAtUtc = state.ExpiresAt,
            BlockedApps = state.BlockedApps.ToArray(),
            LastUpdatedUtc = asOfUtc
        };
    }
}
