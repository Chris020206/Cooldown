namespace Cooldown.Service.State;

public interface ILockStateManager
{
    /// <summary>
    /// Returns the currently active lock, or null if none.
    /// Only one lock can be active at a time.
    /// </summary>
    Task<LockState?> GetCurrentLockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts (or replaces) the active lock with the provided parameters.
    /// Starting a new lock replaces any previous lock.
    /// </summary>
    Task<LockState> StartLockAsync(LockParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels any active lock, leaving the system unlocked.
    /// </summary>
    Task CancelLockAsync(CancellationToken cancellationToken = default);
}
