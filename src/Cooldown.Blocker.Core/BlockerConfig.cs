using System.Text.Json.Serialization;
using Cooldown.Blocker.Core.Processes;
using Microsoft.Extensions.Logging;

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

    public EffectiveBlockSet GetEffectiveBlockSet(Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        logger ??= Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        var selectedRaw = Apps.Where(a => a.Enabled).Select(a => a.Name).ToList();
        var selectedNormalized = Apps.Where(a => a.Enabled)
            .Select(a => new
            {
                Raw = a.Name,
                Normalized = NameNormalizer.NormalizeAppKey(a.Name)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Normalized))
            .ToList();

        var duplicateCount = selectedNormalized.Count - selectedNormalized.Select(x => x.Normalized).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (duplicateCount > 0)
        {
            logger.LogDebug(EventIds.DependencyResolution, "Selected apps contained duplicates after normalization. Duplicates={DuplicateCount} RawSelection={Selection}", duplicateCount, FormatList(selectedRaw));
        }

        var missingDefinitions = new List<string>();
        var definitions = BuildAppDefinitions(missingDefinitions).ToList();
        var definitionMap = definitions.ToDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);
        foreach (var missing in missingDefinitions)
        {
            logger.LogWarning(EventIds.MissingDefinition, "Selected app has no definition; falling back to direct process name. App={App}", missing);
        }

        foreach (var item in selectedNormalized)
        {
            var matchType = definitionMap.ContainsKey(item.Normalized)
                ? (string.Equals(item.Raw, item.Normalized, StringComparison.OrdinalIgnoreCase) ? "Exact" : "Alias/Normalized")
                : "Fallback";
            logger.LogDebug(EventIds.NameNormalization, "App selection normalized. Raw={Raw} Normalized={Normalized} MatchType={MatchType}", item.Raw, item.Normalized, matchType);
        }

        var dependencies = BuildAppDependencies();
        var graph = new AppDependencyGraph(definitions, dependencies, NameNormalizer.NormalizeAppKey);
        var additionalProcessNames = EnabledProcessGroups.SelectMany(g => g.AllProcessNames);
        var effective = graph.Expand(selectedNormalized.Select(x => x.Normalized), additionalProcessNames, logger);

        logger.LogInformation(EventIds.DependencyResolution, "Selected apps (raw)={SelectedRaw} Selected apps (normalized)={SelectedNormalized} Effective apps={EffectiveApps} Effective processes={EffectiveProcesses}", FormatList(selectedRaw), FormatList(selectedNormalized.Select(x => x.Normalized)), FormatList(effective.AppKeys), FormatList(effective.ProcessNames));

        if (EnabledProcessGroups.Count > 0)
        {
            foreach (var group in EnabledProcessGroups)
            {
                logger.LogDebug(EventIds.DependencyResolution, "Process group included: {GroupId} -> {Processes}", group.Id, FormatList(group.AllProcessNames));
            }
        }

        if (effective.ProcessNames.Count == 0)
        {
            logger.LogWarning(EventIds.EmptyEffectiveSet, "Effective blocked process set is empty. Check app definitions/aliases. Selected={Selected}", FormatList(selectedRaw));
        }

        return effective;
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

    private IReadOnlyCollection<AppDefinition> BuildAppDefinitions(List<string>? missingDefinitions = null)
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
                missingDefinitions?.Add(key);
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

    private static string FormatList(IEnumerable<string> items, int cap = 20)
    {
        var list = items.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct(StringComparer.OrdinalIgnoreCase).Take(cap + 1).ToList();
        if (list.Count <= cap)
        {
            return string.Join(", ", list);
        }

        return $"{string.Join(", ", list.Take(cap))} (+{list.Count - cap} more)";
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
