namespace Cooldown.Service.State;

/// <summary>
/// Immutable snapshot of the currently enforced lock.
/// </summary>
public sealed record LockState
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public LockType Type { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public TimeSpan Duration { get; init; }

    public IReadOnlyCollection<string> BlockedApps { get; init; } = Array.Empty<string>();

    public bool IsActive(DateTimeOffset nowUtc) => nowUtc < ExpiresAt;
}
