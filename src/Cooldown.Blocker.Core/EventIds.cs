using Microsoft.Extensions.Logging;

namespace Cooldown.Blocker.Core;

public static class EventIds
{
    // 2400-2499: configuration/dependency/lock lifecycle
    public static readonly EventId DependencyResolution = new(2400, nameof(DependencyResolution));
    public static readonly EventId DependencyCycle = new(2401, nameof(DependencyCycle));
    public static readonly EventId MissingDefinition = new(2402, nameof(MissingDefinition));
    public static readonly EventId EmptyEffectiveSet = new(2403, nameof(EmptyEffectiveSet));
    public static readonly EventId NameNormalization = new(2404, nameof(NameNormalization));
    public static readonly EventId LockStart = new(2410, nameof(LockStart));
    public static readonly EventId LockCleared = new(2411, nameof(LockCleared));

    // 2500-2599: monitoring/termination
    public static readonly EventId ProcessDetected = new(2500, nameof(ProcessDetected));
    public static readonly EventId ProcessCleared = new(2501, nameof(ProcessCleared));
    public static readonly EventId ProcessTerminated = new(2502, nameof(ProcessTerminated));
    public static readonly EventId ProcessTerminationFailed = new(2503, nameof(ProcessTerminationFailed));
    public static readonly EventId ProcessMissing = new(2504, nameof(ProcessMissing));
}
