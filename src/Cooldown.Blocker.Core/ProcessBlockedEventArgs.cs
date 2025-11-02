namespace Cooldown.Blocker.Core;

public class ProcessBlockedEventArgs : EventArgs
{
    public required string ProcessName { get; init; }

    public required int ProcessId { get; init; }

    public required ProcessTerminationResult Result { get; init; }
}
