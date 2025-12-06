using System.Text.Json;

namespace Cooldown.Service.IPC.Protocol;

public sealed class MessageEnvelope
{
    public int ProtocolVersion { get; set; }

    public string MessageType { get; set; } = string.Empty; // "Command" | "Response" | "Event"

    public string Command { get; set; } = string.Empty;

    public string? CorrelationId { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }

    public JsonElement Payload { get; set; }
}
