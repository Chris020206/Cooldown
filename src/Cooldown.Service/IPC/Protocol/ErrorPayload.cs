namespace Cooldown.Service.IPC.Protocol;

public sealed class ErrorPayload
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public object? Details { get; set; }
}
