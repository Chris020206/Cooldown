namespace Cooldown.Blocker.Core;

public sealed record EffectiveBlockSet(
    IReadOnlyCollection<string> AppKeys,
    IReadOnlyCollection<string> ProcessNames);
