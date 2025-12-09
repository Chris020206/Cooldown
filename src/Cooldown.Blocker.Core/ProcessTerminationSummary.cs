namespace Cooldown.Blocker.Core;

public sealed record ProcessTerminationSummary(int TerminatedCount, IReadOnlyCollection<string> TerminatedProcessNames);
