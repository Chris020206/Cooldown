using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cooldown.Desktop.IPC;

public sealed class MessageEnvelope
{
    public int ProtocolVersion { get; set; }

    public string MessageType { get; set; } = string.Empty; // "Command" | "Response" | "Event"

    public string Command { get; set; } = string.Empty;

    public string? CorrelationId { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }

    public JsonElement Payload { get; set; }
}

public sealed class CommandResponse<TPayload>
{
    public bool Success { get; set; }

    public TPayload? Result { get; set; }

    public ErrorPayload? Error { get; set; }
}

public sealed class ErrorPayload
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Details { get; set; }
}
