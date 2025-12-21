using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Cooldown.Blocker.Core;

public static class ProcessTerminator
{
    private static readonly HashSet<string> ProtectedProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cooldown",
        "Cooldown.Desktop",
        "Cooldown.Service",
        "Cooldown.Blocker",
        "Cooldown.Blocker.Core"
    };

    public static ProcessTerminationSummary TerminateRunningBlockedProcesses(
        IEnumerable<string> blockedProcessNames,
        Func<int, string, ProcessTerminationResult> terminator,
        ProcessTerminationOptions? options = null,
        ILogger? logger = null)
    {
        if (blockedProcessNames == null)
        {
            throw new ArgumentNullException(nameof(blockedProcessNames));
        }

        if (terminator == null)
        {
            throw new ArgumentNullException(nameof(terminator));
        }

        options ??= ProcessTerminationOptions.Default;
        logger ??= Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        var attempts = Math.Max(1, options.Attempts);
        var delay = options.DelayBetweenAttempts < TimeSpan.Zero
            ? TimeSpan.Zero
            : options.DelayBetweenAttempts;
        var blocked = new HashSet<string>(
            blockedProcessNames
                .Select(NormalizeName)
                .Where(name => !string.IsNullOrWhiteSpace(name)),
            StringComparer.OrdinalIgnoreCase);

        if (blocked.Count == 0)
        {
            return ProcessTerminationSummary.Empty;
        }

        var currentProcessId = Environment.ProcessId;
        var terminatedCount = 0;
        var terminatedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var failedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            var processSnapshot = CaptureBlockedProcesses(blocked, currentProcessId);
            if (processSnapshot.Count == 0 && attempt == attempts && terminatedCount == 0)
            {
                break;
            }

            foreach (var process in OrderForTermination(processSnapshot))
            {
                try
                {
                    seenTargets.Add(process.NormalizedName);
                    if (!string.IsNullOrWhiteSpace(process.ExecutableBaseName))
                    {
                        seenTargets.Add(process.ExecutableBaseName);
                    }

                    var result = terminator(process.Id, process.DisplayName);
                    if (result.Status == ProcessTerminationStatus.Terminated)
                    {
                        terminatedCount++;
                        terminatedNames.Add(process.DisplayName);
                        logger.LogInformation(EventIds.ProcessTerminated, "Terminated blocked process {ProcessName} (PID {Pid}) MatchBy={MatchBy}", process.DisplayName, process.Id, process.MatchBy);
                    }
                    else if (result.Status == ProcessTerminationStatus.Failed)
                    {
                        failedNames.Add(process.DisplayName);
                        logger.LogWarning(EventIds.ProcessTerminationFailed, "Failed to terminate blocked process {ProcessName} (PID {Pid}) Reason={Reason}", process.DisplayName, process.Id, result.Message);
                    }
                    else if (result.Status == ProcessTerminationStatus.AlreadyExited)
                    {
                        logger.LogDebug(EventIds.ProcessTerminationFailed, "Process already exited before termination {ProcessName} (PID {Pid})", process.DisplayName, process.Id);
                    }
                }
                catch
                {
                    failedNames.Add(process.DisplayName);
                    logger.LogWarning(EventIds.ProcessTerminationFailed, "Failed to terminate blocked process {ProcessName} (PID {Pid}) due to unexpected exception", process.DisplayName, process.Id);
                }
            }

            if (attempt < attempts)
            {
                Thread.Sleep(delay);
            }
        }

        var missing = blocked.Except(seenTargets, StringComparer.OrdinalIgnoreCase).ToList();
        if (missing.Count > 0)
        {
            logger.LogDebug(EventIds.ProcessMissing, "Blocked targets not observed during sweep: {Targets}", FormatList(missing));
        }

        var orderedTerminatedNames = terminatedNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        var orderedFailedNames = failedNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        var summaryMessage = BuildSummaryMessage(terminatedCount, orderedTerminatedNames, orderedFailedNames);

        return new ProcessTerminationSummary(terminatedCount, orderedTerminatedNames, orderedFailedNames, summaryMessage);
    }

    private static List<ProcessInfo> CaptureBlockedProcesses(HashSet<string> blockedNames, int currentProcessId)
    {
        var results = new List<ProcessInfo>();
        var metadata = TryGetProcessMetadata();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var normalizedName = NormalizeName(process.ProcessName);
                if (process.Id == currentProcessId || ProtectedProcessNames.Contains(normalizedName))
                {
                    continue;
                }

                ProcessMetadata? info;
                var hasMetadata = metadata.TryGetValue(process.Id, out info);
                var parentId = hasMetadata ? info!.ParentProcessId : null;
                var executableBaseName = hasMetadata
                    ? info!.ExecutableBaseName
                    : TryGetExecutableBaseName(process);

                if (!string.IsNullOrWhiteSpace(executableBaseName) && ProtectedProcessNames.Contains(executableBaseName))
                {
                    continue;
                }

                var isBlocked = blockedNames.Contains(normalizedName) ||
                                (!string.IsNullOrWhiteSpace(executableBaseName) && blockedNames.Contains(executableBaseName!));

                if (!isBlocked)
                {
                    continue;
                }

                var matchBy = blockedNames.Contains(normalizedName) ? "ProcessName" : "ExecutablePath";
                results.Add(new ProcessInfo(process.Id, process.ProcessName, normalizedName, executableBaseName, parentId, matchBy));
            }
            catch
            {
                // Ignore inspection failures for individual processes.
            }
            finally
            {
                process.Dispose();
            }
        }

        return results;
    }

    private static IReadOnlyCollection<ProcessInfo> OrderForTermination(IReadOnlyCollection<ProcessInfo> processes)
    {
        if (processes.Count == 0)
        {
            return processes;
        }

        var byId = processes.ToDictionary(p => p.Id);
        var depthCache = new Dictionary<int, int>();

        int GetDepth(ProcessInfo info)
        {
            if (depthCache.TryGetValue(info.Id, out var cached))
            {
                return cached;
            }

            var depth = 0;
            var current = info;
            while (current.ParentId.HasValue && byId.TryGetValue(current.ParentId.Value, out var parent))
            {
                depth++;
                current = parent;

                // Guard against cycles just in case.
                if (depth > 10)
                {
                    break;
                }
            }

            depthCache[info.Id] = depth;
            return depth;
        }

        return processes
            .OrderByDescending(GetDepth)
            .ThenBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<int, ProcessMetadata> TryGetProcessMetadata()
    {
        var results = new Dictionary<int, ProcessMetadata>();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId, Name, ExecutablePath FROM Win32_Process");
            foreach (ManagementObject obj in searcher.Get())
            {
                try
                {
                    var pid = Convert.ToInt32(obj["ProcessId"]);
                    var parent = obj["ParentProcessId"] != null ? Convert.ToInt32(obj["ParentProcessId"]) : (int?)null;
                    var name = obj["Name"]?.ToString() ?? string.Empty;
                    var executablePath = obj["ExecutablePath"]?.ToString();
                    var executableBaseName = NormalizeName(
                        !string.IsNullOrWhiteSpace(executablePath)
                            ? Path.GetFileNameWithoutExtension(executablePath)
                            : name);

                    results[pid] = new ProcessMetadata(parent, executableBaseName);
                }
                catch
                {
                    // Ignore malformed entries.
                }
                finally
                {
                    obj.Dispose();
                }
            }
        }
        catch
        {
            // WMI may be unavailable or blocked; fall back to Process inspection only.
        }

        return results;
    }

    private static string? TryGetExecutableBaseName(Process process)
    {
        try
        {
            var path = process.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            return NormalizeName(Path.GetFileNameWithoutExtension(path));
        }
        catch
        {
            return null;
        }
    }

    private static string BuildSummaryMessage(int terminatedCount, IReadOnlyCollection<string> terminatedNames, IReadOnlyCollection<string> failedNames)
    {
        if (terminatedCount == 0 && failedNames.Count == 0)
        {
            return "No blocked apps were running at lock start.";
        }

        var parts = new List<string>();
        if (terminatedCount > 0)
        {
            var suffix = terminatedNames.Count > 0 ? $": {string.Join(", ", terminatedNames)}" : string.Empty;
            parts.Add($"Closed {terminatedCount} blocked app{(terminatedCount == 1 ? string.Empty : "s")}{suffix}");
        }

        if (failedNames.Count > 0)
        {
            parts.Add($"Failed to close: {string.Join(", ", failedNames)}");
        }

        return string.Join(". ", parts);
    }

    private static string NormalizeName(string? name)
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

    private sealed record ProcessInfo(int Id, string DisplayName, string NormalizedName, string? ExecutableBaseName, int? ParentId, string MatchBy);

    private sealed record ProcessMetadata(int? ParentProcessId, string? ExecutableBaseName);

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
