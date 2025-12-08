namespace Cooldown.Service.IPC;

public sealed class LockStateSnapshot
{
    public Guid LockId { get; init; }

    public string Type { get; init; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; init; }

    public DateTimeOffset ExpiresAtUtc { get; init; }

    public int DurationSeconds { get; init; }

    public int RemainingSeconds { get; init; }

    public IReadOnlyCollection<string> BlockedApps { get; init; } = Array.Empty<string>();
}

public sealed class LockStateChangedEventPayload
{
    public bool HasActiveLock { get; init; }

    public LockStateSnapshot? Lock { get; init; }

    public string? Reason { get; init; }
}
