using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cooldown.Blocker.Core;

/// <summary>
/// Resolves selected app keys into their dependency-closure and flattened process list.
/// </summary>
public sealed class AppDependencyGraph
{
    private readonly Dictionary<string, AppDefinition> _definitions;
    private readonly Dictionary<string, List<string>> _edges;
    private readonly HashSet<string> _cycleWarnings = new(StringComparer.OrdinalIgnoreCase);

    public AppDependencyGraph(IEnumerable<AppDefinition> definitions, IEnumerable<AppDependency> dependencies, Func<string, string>? keyNormalizer = null)
    {
        keyNormalizer ??= NameNormalizer.NormalizeAppKey;

        _definitions = definitions
            .Where(d => d != null)
            .Select(CloneAndNormalize)
            .Where(d => !string.IsNullOrWhiteSpace(d.Key))
            .GroupBy(d => d.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(d => d.Key, d => d, StringComparer.OrdinalIgnoreCase);

        _edges = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var dependency in dependencies.Where(d => d != null))
        {
            var dep = CloneAndNormalize(dependency, keyNormalizer);
            if (string.IsNullOrWhiteSpace(dep.SourceKey) || string.IsNullOrWhiteSpace(dep.TargetKey))
            {
                continue;
            }

            if (!_edges.TryGetValue(dep.SourceKey, out var targets))
            {
                targets = new List<string>();
                _edges[dep.SourceKey] = targets;
            }

            if (!targets.Contains(dep.TargetKey, StringComparer.OrdinalIgnoreCase))
            {
                targets.Add(dep.TargetKey);
            }
        }
    }

    public EffectiveBlockSet Expand(IEnumerable<string> selectedAppKeys, IEnumerable<string>? additionalProcessNames = null, ILogger? logger = null)
    {
        logger ??= Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        var selectedList = selectedAppKeys.ToList();
        var normalizedSelected = new HashSet<string>(
            selectedList.Select(NameNormalizer.NormalizeAppKey)
                .Where(key => !string.IsNullOrWhiteSpace(key)),
            StringComparer.OrdinalIgnoreCase);

        var effectiveKeys = new HashSet<string>(normalizedSelected, StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = new Stack<string>();
        var addedByDependency = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in normalizedSelected)
        {
            Traverse(key, effectiveKeys, visited, path, addedByDependency, logger);
        }

        var processNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in effectiveKeys)
        {
            if (_definitions.TryGetValue(key, out var def))
            {
                foreach (var proc in def.ProcessNames)
                {
                    processNames.Add(proc);
                }
            }
        }

        if (additionalProcessNames != null)
        {
            foreach (var proc in additionalProcessNames)
            {
                var normalized = NameNormalizer.NormalizeProcessName(proc);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    processNames.Add(normalized);
                }
            }
        }

        var orderedKeys = effectiveKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        var orderedProcesses = processNames.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();

        if (normalizedSelected.Count != selectedList.Count)
        {
            logger.LogDebug(EventIds.DependencyResolution, "Selected apps contained duplicates that were deduplicated. RawCount={RawCount} DedupedCount={DedupedCount}", selectedList.Count, normalizedSelected.Count);
        }

        foreach (var kvp in addedByDependency)
        {
            logger.LogDebug(EventIds.DependencyResolution, "Dependency expansion added {Target} via {Source}", kvp.Key, kvp.Value);
        }

        logger.LogInformation(EventIds.DependencyResolution, "Resolved app selection. Selected={Selected} EffectiveApps={EffectiveApps} EffectiveProcesses={EffectiveProcesses}", FormatList(normalizedSelected), FormatList(orderedKeys), FormatList(orderedProcesses));

        return new EffectiveBlockSet(orderedKeys, orderedProcesses);
    }

    private void Traverse(
        string key,
        HashSet<string> effectiveKeys,
        HashSet<string> visited,
        Stack<string> path,
        Dictionary<string, string> addedByDependency,
        ILogger logger)
    {
        if (path.Contains(key))
        {
            var cyclePath = $"{string.Join(" -> ", path.Reverse())} -> {key}";
            if (_cycleWarnings.Add(cyclePath))
            {
                logger.LogWarning(EventIds.DependencyCycle, "Cycle detected in dependency graph: {CyclePath}", cyclePath);
            }

            return;
        }

        if (!effectiveKeys.Contains(key))
        {
            effectiveKeys.Add(key);
        }

        if (!visited.Add(key))
        {
            return;
        }

        if (!_edges.TryGetValue(key, out var targets) || targets.Count == 0)
        {
            return;
        }

        path.Push(key);
        foreach (var target in targets)
        {
            if (!effectiveKeys.Contains(target))
            {
                addedByDependency[target] = key;
            }

            Traverse(target, effectiveKeys, visited, path, addedByDependency, logger);
        }

        path.Pop();
    }

    private static AppDefinition CloneAndNormalize(AppDefinition definition)
    {
        var copy = new AppDefinition
        {
            Key = definition.Key,
            DisplayName = definition.DisplayName,
            ProcessNames = definition.ProcessNames.ToList()
        };
        copy.Normalize(NameNormalizer.NormalizeAppKey);
        return copy;
    }

    private static AppDependency CloneAndNormalize(AppDependency dependency, Func<string, string> keyNormalizer)
    {
        var copy = new AppDependency
        {
            SourceKey = dependency.SourceKey,
            TargetKey = dependency.TargetKey,
            Type = dependency.Type
        };
        copy.Normalize(keyNormalizer);
        return copy;
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
