using System.Text.Json.Serialization;

namespace Cooldown.Desktop.IPC;

public sealed class PingResponsePayload
{
    [JsonPropertyName("serviceVersion")]
    public string ServiceVersion { get; set; } = string.Empty;

    [JsonPropertyName("protocolVersion")]
    public int ProtocolVersion { get; set; }

    [JsonPropertyName("uptimeSeconds")]
    public long UptimeSeconds { get; set; }
}
