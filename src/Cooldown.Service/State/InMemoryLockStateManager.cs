using Microsoft.Extensions.Logging;

namespace Cooldown.Service.State;

public sealed class InMemoryLockStateManager : ILockStateManager
{
    private readonly ILogger<InMemoryLockStateManager> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private LockState? _current;

    public InMemoryLockStateManager(ILogger<InMemoryLockStateManager> logger)
    {
        _logger = logger;
    }

    public async Task<LockState?> GetCurrentLockAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
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

        var now = DateTimeOffset.UtcNow;
        var newState = new LockState
        {
            Type = parameters.Type,
            StartedAt = now,
            ExpiresAt = now.Add(parameters.Duration),
            Duration = parameters.Duration,
            BlockedApps = parameters.BlockedApps?.ToArray() ?? Array.Empty<string>()
        };

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _current = newState;
        }
        finally
        {
            _gate.Release();
        }

        _logger.LogInformation("Lock started (type={Type}, duration={Duration}, blockedApps={BlockedApps}).", newState.Type, newState.Duration, newState.BlockedApps.Count);
        return newState;
    }

    public async Task CancelLockAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_current == null)
            {
                return;
            }

            _current = null;
        }
        finally
        {
            _gate.Release();
        }

        _logger.LogInformation("Lock canceled; system is now unlocked.");
    }
}
