namespace Cooldown.Blocker.Core;

public sealed class LockManager
{
    private readonly object _sync = new();
    private LockState? _currentLock;

    public event EventHandler<LockState>? LockStateChanged;

    public LockState CreateLock(int minutes, LockType type)
    {
        if (minutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes));
        }

        var lockState = new LockState
        {
            IsActive = true,
            Type = type,
            DurationMinutes = minutes,
            StartTime = DateTimeOffset.Now,
            EndTime = DateTimeOffset.Now.AddMinutes(minutes)
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
