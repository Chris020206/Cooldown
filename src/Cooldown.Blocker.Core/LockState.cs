namespace Cooldown.Blocker.Core;

public class LockState
{
    public bool IsActive { get; set; }

    public LockType Type { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public int DurationMinutes { get; set; }

    public static LockState Inactive() => new()
    {
        IsActive = false,
        Type = LockType.Soft,
        StartTime = DateTimeOffset.MinValue,
        EndTime = DateTimeOffset.MinValue,
        DurationMinutes = 0
    };

    public LockState Clone() => new()
    {
        IsActive = IsActive,
        Type = Type,
        StartTime = StartTime,
        EndTime = EndTime,
        DurationMinutes = DurationMinutes
    };
}
