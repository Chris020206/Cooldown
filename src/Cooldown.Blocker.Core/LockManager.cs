namespace Cooldown.Blocker.Core;

public sealed class LockManager
{
    private readonly object _sync = new();
    private LockState? _currentLock;

    public event EventHandler<LockState>? LockStateChanged;

    /// <summary>
    /// Creates a new lock for the given duration (minutes), raising state change events.
    /// </summary>
    public LockState CreateLock(int minutes, LockType type)
    {
        if (minutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes));
        }

        return CreateLock(TimeSpan.FromMinutes(minutes), type);
    }

    /// <summary>
    /// Creates a new lock for the given duration (supports second precision), raising state change events.
    /// </summary>
    public LockState CreateLock(TimeSpan duration, LockType type)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        var now = DateTimeOffset.Now;
        var lockState = new LockState
        {
            IsActive = true,
            Type = type,
            DurationMinutes = (int)Math.Ceiling(duration.TotalMinutes),
            StartTime = now,
            EndTime = now.Add(duration)
        };

        SetLock(lockState);
        return lockState.Clone();
    }

    /// <summary>
    /// Applies an externally provided lock window (used when syncing with the service).
    /// </summary>
    public LockState ApplyExternalLock(DateTimeOffset startTime, DateTimeOffset endTime, LockType type)
    {
        if (endTime <= startTime)
        {
            throw new ArgumentOutOfRangeException(nameof(endTime));
        }

        lock (_sync)
        {
            if (_currentLock is { IsActive: true } existing &&
                existing.StartTime == startTime &&
                existing.EndTime == endTime &&
                existing.Type == type)
            {
                return existing.Clone();
            }
        }

        var duration = endTime - startTime;
        var lockState = new LockState
        {
            IsActive = true,
            Type = type,
            DurationMinutes = (int)Math.Ceiling(duration.TotalMinutes),
            StartTime = startTime,
            EndTime = endTime
        };

        SetLock(lockState);
        return lockState.Clone();
    }

    public bool CancelLock()
    {
        LockState? updatedState = null;
        var canceled = false;

        lock (_sync)
        {
            if (_currentLock is { IsActive: true } state)
            {
                if (state.Type == LockType.Hard)
                {
                    return false;
                }

                state.IsActive = false;
                _currentLock = state;
                updatedState = state.Clone();
                canceled = true;
            }
        }

        if (updatedState != null)
        {
            RaiseLockStateChanged(updatedState);
        }

        return canceled;
    }

    public LockState GetStatus()
    {
        IsLockEnforced();

        lock (_sync)
        {
            if (_currentLock == null)
            {
                return LockState.Inactive();
            }

            var clone = _currentLock.Clone();
            if (!clone.IsActive)
            {
                clone.EndTime = clone.EndTime == DateTimeOffset.MinValue ? DateTimeOffset.MinValue : clone.EndTime;
            }

            return clone;
        }
    }

    public bool IsLockEnforced()
    {
        LockState? changedState = null;
        var isActive = false;

        lock (_sync)
        {
            if (_currentLock is { } state)
            {
                if (!state.IsActive)
                {
                    return false;
                }

                if (DateTimeOffset.Now >= state.EndTime)
                {
                    state.IsActive = false;
                    _currentLock = state;
                    changedState = state.Clone();
                }
                else
                {
                    isActive = true;
                }
            }
        }

        if (changedState != null)
        {
            RaiseLockStateChanged(changedState);
        }

        return isActive;
    }

    /// <summary>
    /// Forces the current lock (if any) to inactive state. Used when syncing to a service-canceled/expired lock.
    /// </summary>
    public void ForceClearLock()
    {
        LockState? changedState = null;
        lock (_sync)
        {
            if (_currentLock is { IsActive: true })
            {
                _currentLock.IsActive = false;
                changedState = _currentLock.Clone();
            }
        }

        if (changedState != null)
        {
            RaiseLockStateChanged(changedState);
        }
    }

    public async Task RunTimerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                IsLockEnforced();
                await Task.Delay(1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void SetLock(LockState newLock)
    {
        lock (_sync)
        {
            _currentLock = newLock;
        }

        RaiseLockStateChanged(newLock.Clone());
    }

    private void RaiseLockStateChanged(LockState state)
    {
        LockStateChanged?.Invoke(this, state);
    }
}
