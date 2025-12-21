using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cooldown.Blocker.Core;

public sealed class ProcessMonitor
{
    private readonly object _namesLock = new();
    private readonly Dictionary<int, string> _seenBlocked = new();
    private readonly object _seenLock = new();
    private HashSet<string> _blockedNames;
    private int _checkIntervalMs;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    public ProcessMonitor(IEnumerable<string> blockedProcessNames, int checkIntervalMs, Microsoft.Extensions.Logging.ILogger logger)
    {
        _blockedNames = CreateNameSet(blockedProcessNames);
        _checkIntervalMs = checkIntervalMs;
        _logger = logger;
    }

    public event EventHandler<ProcessDetectedEventArgs>? ProcessDetected;

    public void UpdateTargets(IEnumerable<string> blockedProcessNames)
    {
        lock (_namesLock)
        {
            _blockedNames = CreateNameSet(blockedProcessNames);
        }
    }

    public void UpdateCheckInterval(int checkIntervalMs)
    {
        _checkIntervalMs = checkIntervalMs;
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                ScanProcesses();
                await Task.Delay(_checkIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Swallow exceptions from process scanning to keep the monitor alive.
            }
        }
    }

    private void ScanProcesses()
    {
        var processes = Process.GetProcesses();
        var currentBlockedPids = new HashSet<int>();

        HashSet<string> blockedSnapshot;
        lock (_namesLock)
        {
            blockedSnapshot = _blockedNames;
        }

        foreach (var proc in processes)
        {
            try
            {
                var name = proc.ProcessName;
                var shouldNotify = false;

                if (blockedSnapshot.Contains(name))
                {
                    currentBlockedPids.Add(proc.Id);
                    lock (_seenLock)
                    {
                        if (!_seenBlocked.ContainsKey(proc.Id))
                        {
                            _seenBlocked[proc.Id] = proc.ProcessName;
                            shouldNotify = true;
                        }
                    }
                }

                if (shouldNotify)
                {
                    _logger.LogInformation(EventIds.ProcessDetected, "Detected blocked process {ProcessName} (PID {Pid})", proc.ProcessName, proc.Id);
                    ProcessDetected?.Invoke(this, new ProcessDetectedEventArgs
                    {
                        ProcessId = proc.Id,
                        ProcessName = proc.ProcessName
                    });
                }
            }
            catch
            {
                // Process may have exited between enumeration and inspection.
            }
            finally
            {
                proc.Dispose();
            }
        }

        List<(int Id, string Name)>? removed = null;
        lock (_seenLock)
        {
            foreach (var kvp in _seenBlocked)
            {
                if (!currentBlockedPids.Contains(kvp.Key))
                {
                    removed ??= new List<(int, string)>();
                    removed.Add((kvp.Key, kvp.Value));
                }
            }

            if (removed != null)
            {
                foreach (var item in removed)
                {
                    _seenBlocked.Remove(item.Id);
                }
            }
        }

        if (removed != null)
        {
            foreach (var item in removed)
            {
                _logger.LogInformation(EventIds.ProcessCleared, "Blocked process exited {ProcessName} (PID {Pid})", item.Name, item.Id);
            }
        }
    }

    private static HashSet<string> CreateNameSet(IEnumerable<string> names)
    {
        return new HashSet<string>(names.Select(NormalizeName), StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeName(string name)
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
}
