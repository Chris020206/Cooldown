namespace Cooldown.Desktop.ViewModels;

public class ProcessEventViewModel
{
    public required DateTimeOffset Timestamp { get; init; }

    public required string ProcessName { get; init; }

    public required string Message { get; init; }
}
