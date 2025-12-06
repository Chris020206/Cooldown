namespace Cooldown.Service.IPC.Protocol;

public sealed class CommandResponse
{
    public bool Success { get; set; }

    public object? Result { get; set; }

    public ErrorPayload? Error { get; set; }

    public static CommandResponse FromError(string code, string message)
    {
        return new CommandResponse
        {
            Success = false,
            Error = new ErrorPayload
            {
                Code = code,
                Message = message
            }
        };
    }
}
