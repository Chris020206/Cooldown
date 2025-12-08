namespace Cooldown.Persistence;

public interface ILockStateRepository
{
    Task<LockStateRecord?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(LockStateRecord state, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
