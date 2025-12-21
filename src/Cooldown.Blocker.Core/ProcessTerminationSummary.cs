namespace Cooldown.Blocker.Core;

public sealed record ProcessTerminationSummary(
    int TerminatedCount,
    IReadOnlyCollection<string> TerminatedProcessNames,
    IReadOnlyCollection<string> FailedProcessNames,
    string SummaryMessage)
{
    public static ProcessTerminationSummary Empty { get; } =
        new(0, Array.Empty<string>(), Array.Empty<string>(), "No blocked apps were running at lock start.");

    public bool HasFailures => FailedProcessNames.Count > 0;
}
