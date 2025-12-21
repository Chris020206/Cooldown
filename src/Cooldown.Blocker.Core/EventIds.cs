using Microsoft.Extensions.Logging;

namespace Cooldown.Blocker.Core;

public static class EventIds
{
    public static readonly EventId DependencyResolution = new(2000, nameof(DependencyResolution));
    public static readonly EventId DependencyCycle = new(2001, nameof(DependencyCycle));
    public static readonly EventId MissingDefinition = new(2002, nameof(MissingDefinition));
    public static readonly EventId EmptyEffectiveSet = new(2003, nameof(EmptyEffectiveSet));
    public static readonly EventId NameNormalization = new(2004, nameof(NameNormalization));
    public static readonly EventId LockStart = new(2010, nameof(LockStart));
    public static readonly EventId LockCleared = new(2011, nameof(LockCleared));
    public static readonly EventId ProcessDetected = new(2020, nameof(ProcessDetected));
    public static readonly EventId ProcessTerminated = new(2021, nameof(ProcessTerminated));
    public static readonly EventId ProcessTerminationFailed = new(2022, nameof(ProcessTerminationFailed));
    public static readonly EventId ProcessMissing = new(2023, nameof(ProcessMissing));
}
