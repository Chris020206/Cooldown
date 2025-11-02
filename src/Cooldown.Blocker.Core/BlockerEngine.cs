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
        config.Normalize();
        _config = config;
        _monitor = new ProcessMonitor(config.EnabledProcessNames, config.CheckIntervalMs);
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

        if (state.IsActive)
        {
            var terminated = TerminateExistingBlockedProcesses();
            if (terminated > 0)
            {
                PreExistingProcessesTerminated?.Invoke(this, new PreExistingProcessesTerminatedEventArgs(type, terminated));
            }
        }

        return state;
    }

    public bool CancelLock()
    {
        return _lockManager.CancelLock();
    }

    public LockState GetStatus() => _lockManager.GetStatus();

    public void UpdateConfig(BlockerConfig config)
    {
        config.Normalize();
        _config = config;
        _monitor.UpdateTargets(config.EnabledProcessNames);
        _monitor.UpdateCheckInterval(config.CheckIntervalMs);
    }

    private void OnProcessDetected(object? sender, ProcessDetectedEventArgs e)
    {
        if (!_lockManager.IsLockEnforced())
        {
            return;
        }

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
        var count = 0;

        foreach (var process in _monitor.GetExistingBlockedProcesses())
        {
            var result = HandleProcessTermination(process.ProcessId, process.ProcessName);
            if (result.Status == ProcessTerminationStatus.Terminated)
            {
                count++;
            }
        }

        return count;
    }

    private ProcessTerminationResult HandleProcessTermination(int processId, string processName)
    {
        var result = ProcessKiller.TerminateProcess(processId, processName);
        ProcessBlocked?.Invoke(this, new ProcessBlockedEventArgs
        {
            ProcessId = processId,
            ProcessName = processName,
            Result = result
        });

        return result;
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
