using System.Text.Json.Serialization;

namespace Cooldown.Desktop.IPC;

public sealed class LockCreateRequest
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "Soft" | "Hard"

    [JsonPropertyName("durationSeconds")]
    public int DurationSeconds { get; set; }

    [JsonPropertyName("blockedApps")]
    public string[]? BlockedApps { get; set; }
}

public sealed class LockCreateResponse
{
    [JsonPropertyName("lockId")]
    public string? LockId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("startedAtUtc")]
    public DateTimeOffset StartedAtUtc { get; set; }

    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset ExpiresAtUtc { get; set; }

    [JsonPropertyName("durationSeconds")]
    public int DurationSeconds { get; set; }

    [JsonPropertyName("remainingSeconds")]
    public int RemainingSeconds { get; set; }

    [JsonPropertyName("blockedApps")]
    public string[]? BlockedApps { get; set; }
}

public sealed class LockCancelRequest
{
}

public sealed class LockCancelResponse
{
    [JsonPropertyName("canceled")]
    public bool Canceled { get; set; }

    [JsonPropertyName("previousLockId")]
    public string? PreviousLockId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public sealed class LockStateResponse
{
    [JsonPropertyName("hasActiveLock")]
    public bool HasActiveLock { get; set; }

    [JsonPropertyName("lock")]
    public LockStateDto? Lock { get; set; }
}

public sealed class LockStateDto
{
    [JsonPropertyName("lockId")]
    public string? LockId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("startedAtUtc")]
    public DateTimeOffset StartedAtUtc { get; set; }

    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset ExpiresAtUtc { get; set; }

    [JsonPropertyName("durationSeconds")]
    public int DurationSeconds { get; set; }

    [JsonPropertyName("remainingSeconds")]
    public int RemainingSeconds { get; set; }

    [JsonPropertyName("blockedApps")]
    public string[]? BlockedApps { get; set; }
}
