using System.Linq;

namespace Cooldown.Blocker.Core;

public sealed class BlockerEngine : IAsyncDisposable
{
    private readonly ProcessMonitor _monitor;
    private readonly LockManager _lockManager;
    private BlockerConfig _config;
    private CancellationTokenSource? _cts;
    private Task? _monitorTask;
    private Task? _lockTask;

    public BlockerEngine(BlockerConfig config)
    {
        // Pipeline overview:
        // 1) Config drives the effective blocked set (flat + process groups).
        // 2) ProcessMonitor watches running processes and raises detections.
        // 3) LockManager tracks active locks; BlockerEngine enforces by killing matches (pre-existing + new).
        config.Normalize();
        _config = config;
        _monitor = new ProcessMonitor(config.EnabledProcessNamesWithGroups, config.CheckIntervalMs);
        _monitor.ProcessDetected += OnProcessDetected;

        _lockManager = new LockManager();
        _lockManager.LockStateChanged += OnLockStateChanged;
    }

    public event EventHandler<LockStateChangedEventArgs>? LockStateChanged;

    public event EventHandler<ProcessBlockedEventArgs>? ProcessBlocked;

    public event EventHandler<PreExistingProcessesTerminatedEventArgs>? PreExistingProcessesTerminated;

    public async Task StartAsync()
    {
        if (_cts != null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _monitorTask = Task.Run(() => _monitor.StartMonitoringAsync(_cts.Token));
        _lockTask = Task.Run(() => _lockManager.RunTimerAsync(_cts.Token));
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts == null)
        {
            return;
        }

        _cts.Cancel();

        if (_monitorTask != null)
        {
            await Task.WhenAny(_monitorTask, Task.Delay(500));
        }

        if (_lockTask != null)
        {
            await Task.WhenAny(_lockTask, Task.Delay(500));
        }

        _cts.Dispose();
        _cts = null;
        _monitorTask = null;
        _lockTask = null;
    }

    public LockState CreateLock(int minutes, LockType type)
    {
        var state = _lockManager.CreateLock(minutes, type);
        return OnLockActivated(state, "local-create");
    }

    public bool CancelLock()
    {
        return _lockManager.CancelLock();
    }

    public LockState GetStatus() => _lockManager.GetStatus();

    public LockState ApplyServiceLock(LockType type, DateTimeOffset startedAtUtc, DateTimeOffset expiresAtUtc)
    {
        var state = _lockManager.ApplyExternalLock(startedAtUtc.ToLocalTime(), expiresAtUtc.ToLocalTime(), type);
        return OnLockActivated(state, "service-sync");
    }

    public void ClearServiceLock()
    {
        _lockManager.ForceClearLock();
    }

    public void UpdateConfig(BlockerConfig config)
    {
        config.Normalize();
        _config = config;
        _monitor.UpdateTargets(config.EnabledProcessNamesWithGroups);
        _monitor.UpdateCheckInterval(config.CheckIntervalMs);
    }

    private void OnProcessDetected(object? sender, ProcessDetectedEventArgs e)
    {
        if (!_lockManager.IsLockEnforced())
        {
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[Monitor] Detected blocked process {e.ProcessName} (PID {e.ProcessId}).");
        HandleProcessTermination(e.ProcessId, e.ProcessName);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _monitor.ProcessDetected -= OnProcessDetected;
        _lockManager.LockStateChanged -= OnLockStateChanged;
    }

    private void OnLockStateChanged(object? sender, LockState state)
    {
        LockStateChanged?.Invoke(this, new LockStateChangedEventArgs(state.Clone()));
    }

    private int TerminateExistingBlockedProcesses()
    {
        if (_config == null)
        {
            return 0;
        }

        var targets = _config.EnabledProcessNamesWithGroups.ToList();
        if (targets.Count == 0)
        {
            return 0;
        }

        System.Diagnostics.Debug.WriteLine($"[LockStart] Pre-existing scan. Blocked set ({targets.Count}): {string.Join(", ", targets)}");
        return ProcessTerminator.TerminateExistingProcesses(targets, HandleProcessTermination);
    }

    private ProcessTerminationResult HandleProcessTermination(int processId, string processName)
    {
        var groupId = ResolveGroup(processName);
        System.Diagnostics.Debug.WriteLine($"[Kill] Attempting to terminate {processName} (PID {processId}) group={groupId ?? "none"}");

        var result = ProcessKiller.TerminateProcess(processId, processName);
        System.Diagnostics.Debug.WriteLine($"[Kill] Result for {processName} (PID {processId}): {result.Status} - {result.Message}");
        ProcessBlocked?.Invoke(this, new ProcessBlockedEventArgs
        {
            ProcessId = processId,
            ProcessName = processName,
            Result = result
        });

        return result;
    }

    private LockState OnLockActivated(LockState state, string source)
    {
        if (state.IsActive)
        {
            var terminated = TerminateExistingBlockedProcesses();
            PreExistingProcessesTerminated?.Invoke(this, new PreExistingProcessesTerminatedEventArgs(state.Type, terminated));
            System.Diagnostics.Debug.WriteLine($"[LockStart] Source={source}, terminated {terminated} pre-existing processes.");
        }

        return state;
    }

    private string? ResolveGroup(string processName)
    {
        if (_config?.EnabledProcessGroups == null)
        {
            return null;
        }

        foreach (var group in _config.EnabledProcessGroups)
        {
            if (group.AllProcessNames.Any(name => string.Equals(name, processName, StringComparison.OrdinalIgnoreCase)))
            {
                return group.Id;
            }
        }

        return null;
    }
}

public class LockStateChangedEventArgs : EventArgs
{
    public LockStateChangedEventArgs(LockState state)
    {
        State = state;
    }

    public LockState State { get; }
}

public class PreExistingProcessesTerminatedEventArgs : EventArgs
{
    public PreExistingProcessesTerminatedEventArgs(LockType type, int terminatedCount)
    {
        LockType = type;
        TerminatedCount = terminatedCount;
    }

    public LockType LockType { get; }

    public int TerminatedCount { get; }
}
