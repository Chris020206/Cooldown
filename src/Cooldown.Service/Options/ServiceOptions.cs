namespace Cooldown.Service.Options;

public sealed class ServiceOptions
{
    /// <summary>
    /// Controls the heartbeat delay for the worker loop. Single source of truth for the service cadence.
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 10;
}
