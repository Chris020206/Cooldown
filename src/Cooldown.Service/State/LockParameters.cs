namespace Cooldown.Service.State;

public sealed record LockParameters
{
    public LockType Type { get; init; } = LockType.Soft;

    public TimeSpan Duration { get; init; }

    public IReadOnlyCollection<string> BlockedApps { get; init; } = Array.Empty<string>();
}
