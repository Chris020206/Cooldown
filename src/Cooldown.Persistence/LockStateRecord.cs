namespace Cooldown.Persistence;

public sealed class LockStateRecord
{
    public bool IsActive { get; init; }
    public string LockType { get; init; } = string.Empty;
    public int DurationSeconds { get; init; }
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public IReadOnlyList<string> BlockedApps { get; init; } = Array.Empty<string>();
    public DateTimeOffset LastUpdatedUtc { get; init; }
}
