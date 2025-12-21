namespace Cooldown.Blocker.Core;

/// <summary>
/// Built-in catalog of common games/launchers and their dependency edges.
/// Extend this list to add new games/launchers and their helper processes.
/// </summary>
public static class DefaultAppRegistry
{
    public static IReadOnlyCollection<AppDefinition> AppDefinitions { get; } = new List<AppDefinition>
    {
        new()
        {
            Key = "League of Legends",
            DisplayName = "League of Legends",
            ProcessNames = new List<string> { "LeagueClientUx", "LeagueClientUxRender" }
        },
        new()
        {
            Key = "League",
            DisplayName = "League (alias)",
            ProcessNames = new List<string>()
        },
        new()
        {
            Key = "RiotClientServices",
            DisplayName = "Riot Client",
            ProcessNames = new List<string> { "RiotClientServices" }
        },
        new()
        {
            Key = "Counter-Strike 2",
            DisplayName = "Counter-Strike 2",
            ProcessNames = new List<string> { "cs2" }
        },
        new()
        {
            Key = "Steam",
            DisplayName = "Steam",
            ProcessNames = new List<string> { "steam", "steamservice", "steamwebhelper" }
        }
    };

    public static IReadOnlyCollection<AppDependency> AppDependencies { get; } = new List<AppDependency>
    {
        new()
        {
            SourceKey = "League of Legends",
            TargetKey = "RiotClientServices",
            Type = AppDependencyType.Requires
        },
        new()
        {
            SourceKey = "League",
            TargetKey = "League of Legends",
            Type = AppDependencyType.Requires
        },
        new()
        {
            SourceKey = "Counter-Strike 2",
            TargetKey = "Steam",
            Type = AppDependencyType.Requires
        }
    };
}
