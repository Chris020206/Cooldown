namespace Cooldown.Blocker.Core;

public sealed class ProcessTerminationOptions
{
    public static ProcessTerminationOptions Default { get; } = new();

    /// <summary>
    /// Number of attempts (including the initial pass) to terminate matching processes.
    /// </summary>
    public int Attempts { get; init; } = 3;

    /// <summary>
    /// Delay between retry attempts.
    /// </summary>
    public TimeSpan DelayBetweenAttempts { get; init; } = TimeSpan.FromSeconds(1);
}
