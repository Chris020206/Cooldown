namespace Cooldown.Blocker.Core;

public enum AppDependencyType
{
    Requires = 0
}

/// <summary>
/// Declares that SourceKey implies blocking TargetKey (e.g., League depends on Riot launcher).
/// </summary>
public sealed class AppDependency
{
    public string SourceKey { get; set; } = string.Empty;

    public string TargetKey { get; set; } = string.Empty;

    public AppDependencyType Type { get; set; } = AppDependencyType.Requires;

    public void Normalize(Func<string, string> keyNormalizer)
    {
        SourceKey = keyNormalizer(SourceKey);
        TargetKey = keyNormalizer(TargetKey);
    }
}
