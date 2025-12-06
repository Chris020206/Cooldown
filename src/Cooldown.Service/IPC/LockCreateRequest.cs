using System.Text.Json.Serialization;
using Cooldown.Service.State;

namespace Cooldown.Service.IPC;

public sealed class LockCreateRequest
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("durationSeconds")]
    public int DurationSeconds { get; set; }

    [JsonPropertyName("blockedApps")]
    public string[]? BlockedApps { get; set; }
}
