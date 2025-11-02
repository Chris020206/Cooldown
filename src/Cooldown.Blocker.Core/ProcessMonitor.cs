using System.Diagnostics;

namespace Cooldown.Blocker.Core;

public sealed class ProcessMonitor
{
    private readonly object _namesLock = new();
    private readonly HashSet<int> _seenProcessIds = new();
    private readonly object _seenLock = new();
    private HashSet<string> _blockedNames;
    private int _checkIntervalMs;

    public ProcessMonitor(IEnumerable<string> blockedProcessNames, int checkIntervalMs)
    {
        _blockedNames = CreateNameSet(blockedProcessNames);
        _checkIntervalMs = checkIntervalMs;
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
        var currentPids = new HashSet<int>();

        HashSet<string> blockedSnapshot;
        lock (_namesLock)
        {
            blockedSnapshot = _blockedNames;
        }

        foreach (var proc in processes)
        {
            try
            {
                currentPids.Add(proc.Id);

                var name = proc.ProcessName;
                var shouldNotify = false;

                if (blockedSnapshot.Contains(name))
                {
                    lock (_seenLock)
                    {
                        if (!_seenProcessIds.Contains(proc.Id))
                        {
                            _seenProcessIds.Add(proc.Id);
                            shouldNotify = true;
                        }
                    }
                }

                if (shouldNotify)
                {
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

        lock (_seenLock)
        {
            _seenProcessIds.RemoveWhere(pid => !currentPids.Contains(pid));
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
