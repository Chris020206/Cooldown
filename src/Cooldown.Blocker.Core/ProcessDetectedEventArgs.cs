namespace Cooldown.Blocker.Core;

public class ProcessDetectedEventArgs : EventArgs
{
    public string ProcessName { get; init; } = string.Empty;

    public int ProcessId { get; init; }
}
