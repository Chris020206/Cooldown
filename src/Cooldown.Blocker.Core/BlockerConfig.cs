using System.Text.Json.Serialization;
using Cooldown.Blocker.Core.Processes;

namespace Cooldown.Blocker.Core;

public class BlockerConfig
{
    [JsonPropertyName("apps")]
    public List<BlockableApp> Apps { get; set; } = new();

    [JsonPropertyName("blockedProcessNames")]
    public List<string>? LegacyBlockedProcessNames { get; set; }

    [JsonPropertyName("processGroups")]
    public List<ProcessGroup>? ProcessGroups { get; set; }

    [JsonPropertyName("checkIntervalMs")]
    public int CheckIntervalMs { get; set; } = 1000;

    [JsonPropertyName("enableToastNotifications")]
    public bool EnableToastNotifications { get; set; } = true;

    /// <summary>
    /// Primary flat list of enabled process names used by the current blocking engine.
    /// </summary>
    public IEnumerable<string> EnabledProcessNames => Apps
        .Where(app => app.Enabled)
        .SelectMany(app => ExpandProcessName(NormalizeProcessName(app.Name)))
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Optional richer process groups (game/launcher mappings) for future blocking logic.
    /// </summary>
    public IReadOnlyList<ProcessGroup> EnabledProcessGroups => ProcessGroups?.Where(group => group.Enabled).ToList()
        ?? new List<ProcessGroup>();

    /// <summary>
    /// Combined process names from flat apps and enabled process groups (primary + dependencies).
    /// </summary>
    public IReadOnlyCollection<string> EnabledProcessNamesWithGroups
    {
        get
        {
            var names = new HashSet<string>(EnabledProcessNames, StringComparer.OrdinalIgnoreCase);

            foreach (var group in EnabledProcessGroups)
            {
                foreach (var name in group.AllProcessNames)
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    names.Add(name);
                }
            }

            return names;
        }
    }

    public void Normalize()
    {
        if ((Apps == null || Apps.Count == 0) && LegacyBlockedProcessNames is { Count: > 0 })
        {
            Apps = LegacyBlockedProcessNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => new BlockableApp { Name = name.Trim(), Enabled = true })
                .ToList();
            LegacyBlockedProcessNames = null;
        }

        Apps ??= new List<BlockableApp>();
        foreach (var app in Apps)
        {
            if (string.IsNullOrWhiteSpace(app.Name))
            {
                app.Name = string.Empty;
                continue;
            }

            app.Name = NormalizeProcessName(app.Name);
        }

        ProcessGroups ??= new List<ProcessGroup>();
        foreach (var group in ProcessGroups)
        {
            group.Normalize(NormalizeProcessName);
        }
    }

    public static BlockerConfig CreateDefault()
    {
        return new BlockerConfig
        {
            Apps = new List<BlockableApp>
            {
                new("League of Legends"),
                new("RiotClientServices"),
                new("VALORANT"),
                new("steam"),
                new("steamwebhelper"),
                new("EpicGamesLauncher"),
                new("Battle.net"),
                new("Overwatch"),
                new("FortniteClient-Win64-Shipping"),
                new("csgo"),
                new("dota2")
            },
            CheckIntervalMs = 1000,
            EnableToastNotifications = true,
            ProcessGroups = new List<ProcessGroup>()
        };
    }

    private static string NormalizeProcessName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmed = name.Trim();
        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }

    private static IEnumerable<string> ExpandProcessName(string normalizedName)
    {
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return Array.Empty<string>();
        }

        // Friendly aliases to real process names for common titles.
        return normalizedName.ToLowerInvariant() switch
        {
            "league of legends" => new[] { "LeagueClientUx", "LeagueClientUxRender" },
            "league" => new[] { "LeagueClientUx", "LeagueClientUxRender" },
            _ => new[] { normalizedName }
        };
    }
}

public class BlockableApp
{
    public BlockableApp()
    {
    }

    public BlockableApp(string name, bool enabled = true)
    {
        Name = name;
        Enabled = enabled;
    }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}
