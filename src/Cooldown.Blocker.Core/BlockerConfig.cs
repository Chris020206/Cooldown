using System.Text.Json.Serialization;
using Cooldown.Blocker.Core.Processes;

namespace Cooldown.Blocker.Core;

public class BlockerConfig
{
    [JsonPropertyName("apps")]
    public List<BlockableApp> Apps { get; set; } = new();

    [JsonPropertyName("appDefinitions")]
    public List<AppDefinition> AppDefinitions { get; set; } = new();

    [JsonPropertyName("appDependencies")]
    public List<AppDependency> AppDependencies { get; set; } = new();

    [JsonPropertyName("blockedProcessNames")]
    public List<string>? LegacyBlockedProcessNames { get; set; }

    [JsonPropertyName("processGroups")]
    public List<ProcessGroup>? ProcessGroups { get; set; }

    [JsonPropertyName("checkIntervalMs")]
    public int CheckIntervalMs { get; set; } = 1000;

    [JsonPropertyName("enableToastNotifications")]
    public bool EnableToastNotifications { get; set; } = true;

    public IReadOnlyCollection<string> SelectedAppKeys => Apps
        .Where(app => app.Enabled)
        .Select(app => NameNormalizer.NormalizeAppKey(app.Name))
        .Where(key => !string.IsNullOrWhiteSpace(key))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    /// <summary>
    /// Optional richer process groups (game/launcher mappings) for future blocking logic.
    /// </summary>
    public IReadOnlyList<ProcessGroup> EnabledProcessGroups => ProcessGroups?.Where(group => group.Enabled).ToList()
        ?? new List<ProcessGroup>();

    public EffectiveBlockSet GetEffectiveBlockSet(Action<string>? logWarning = null)
    {
        var graph = new AppDependencyGraph(BuildAppDefinitions(), BuildAppDependencies(), NameNormalizer.NormalizeAppKey);
        var additionalProcessNames = EnabledProcessGroups.SelectMany(g => g.AllProcessNames);
        return graph.Expand(SelectedAppKeys, additionalProcessNames, logWarning);
    }

    /// <summary>
    /// Combined process names from selected apps + dependencies + enabled process groups.
    /// </summary>
    public IReadOnlyCollection<string> EffectiveProcessNames => GetEffectiveBlockSet().ProcessNames;

    /// <summary>
    /// Combined app keys from selected apps + dependencies.
    /// </summary>
    public IReadOnlyCollection<string> EffectiveAppKeys => GetEffectiveBlockSet().AppKeys;

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

            app.Name = NameNormalizer.NormalizeAppKey(app.Name);
        }

        AppDefinitions ??= new List<AppDefinition>();
        foreach (var def in AppDefinitions)
        {
            def.Normalize(NameNormalizer.NormalizeAppKey);
        }

        AppDependencies ??= new List<AppDependency>();
        foreach (var dep in AppDependencies)
        {
            dep.Normalize(NameNormalizer.NormalizeAppKey);
        }

        ProcessGroups ??= new List<ProcessGroup>();
        foreach (var group in ProcessGroups)
        {
            group.Normalize(NameNormalizer.NormalizeProcessName);
        }
    }

    public static BlockerConfig CreateDefault()
    {
        return new BlockerConfig
        {
            Apps = new List<BlockableApp>
            {
                new("League of Legends"),
                new("Counter-Strike 2"),
                new("Steam"),
                new("VALORANT"),
                new("EpicGamesLauncher"),
                new("Battle.net"),
                new("Overwatch"),
                new("FortniteClient-Win64-Shipping"),
                new("csgo"),
                new("dota2")
            },
            AppDefinitions = DefaultAppRegistry.AppDefinitions.ToList(),
            AppDependencies = DefaultAppRegistry.AppDependencies.ToList(),
            CheckIntervalMs = 1000,
            EnableToastNotifications = true,
            ProcessGroups = new List<ProcessGroup>()
        };
    }

    private IReadOnlyCollection<AppDefinition> BuildAppDefinitions()
    {
        var definitions = new Dictionary<string, AppDefinition>(StringComparer.OrdinalIgnoreCase);

        void AddRange(IEnumerable<AppDefinition> source)
        {
            foreach (var def in source)
            {
                if (def == null)
                {
                    continue;
                }

                var copy = new AppDefinition
                {
                    Key = def.Key,
                    DisplayName = def.DisplayName,
                    ProcessNames = def.ProcessNames.ToList()
                };
                copy.Normalize(NameNormalizer.NormalizeAppKey);

                if (string.IsNullOrWhiteSpace(copy.Key))
                {
                    continue;
                }

                definitions[copy.Key] = copy;
            }
        }

        AddRange(DefaultAppRegistry.AppDefinitions);
        AddRange(AppDefinitions);

        // Legacy compatibility: any enabled app with no definition maps directly to a process of the same name.
        foreach (var key in SelectedAppKeys)
        {
            if (!definitions.ContainsKey(key))
            {
                definitions[key] = new AppDefinition
                {
                    Key = key,
                    DisplayName = key,
                    ProcessNames = new List<string> { key }
                };
            }
        }

        return definitions.Values;
    }

    private IReadOnlyCollection<AppDependency> BuildAppDependencies()
    {
        var dependencies = new Dictionary<string, AppDependency>(StringComparer.OrdinalIgnoreCase);

        void AddRange(IEnumerable<AppDependency> source)
        {
            foreach (var dep in source)
            {
                if (dep == null)
                {
                    continue;
                }

                var copy = new AppDependency
                {
                    SourceKey = dep.SourceKey,
                    TargetKey = dep.TargetKey,
                    Type = dep.Type
                };
                copy.Normalize(NameNormalizer.NormalizeAppKey);
                if (string.IsNullOrWhiteSpace(copy.SourceKey) || string.IsNullOrWhiteSpace(copy.TargetKey))
                {
                    continue;
                }

                var hash = $"{copy.SourceKey}=>{copy.TargetKey}";
                if (!dependencies.ContainsKey(hash))
                {
                    dependencies[hash] = copy;
                }
            }
        }

        AddRange(DefaultAppRegistry.AppDependencies);
        AddRange(AppDependencies);
        return dependencies.Values;
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
