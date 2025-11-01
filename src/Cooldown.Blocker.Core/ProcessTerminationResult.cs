namespace Cooldown.Blocker.Core;

public enum ProcessTerminationStatus
{
    Terminated,
    AlreadyExited,
    Failed
}

public record ProcessTerminationResult(ProcessTerminationStatus Status, string Message)
{
    public static ProcessTerminationResult Terminated(string processName) =>
        new(ProcessTerminationStatus.Terminated, $"Terminated {processName}");

    public static ProcessTerminationResult AlreadyExited(string processName) =>
        new(ProcessTerminationStatus.AlreadyExited, $"{processName} was already closed");

    public static ProcessTerminationResult Failed(string processName, string reason) =>
        new(ProcessTerminationStatus.Failed, $"Failed to close {processName}: {reason}");
}
