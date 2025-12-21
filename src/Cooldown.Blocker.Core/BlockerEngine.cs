using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cooldown.Blocker.Core;

public sealed class BlockerEngine : IAsyncDisposable
{
    private readonly ProcessMonitor _monitor;
    private readonly LockManager _lockManager;
    private readonly ILogger<BlockerEngine> _logger;
    private BlockerConfig _config;
    private CancellationTokenSource? _cts;
    private Task? _monitorTask;
    private Task? _lockTask;
    private string? _appliedLockKey;
    private string? _lastLockSweepKey;

    public BlockerEngine(BlockerConfig config, ILogger<BlockerEngine>? logger = null)
    {
        // Pipeline overview:
        // 1) Config drives the effective blocked set (flat + process groups).
        // 2) ProcessMonitor watches running processes and raises detections.
        // 3) LockManager tracks active locks; BlockerEngine enforces by killing matches (pre-existing + new).
        _logger = logger ?? NullLogger<BlockerEngine>.Instance;
        config.Normalize();
        _config = config;
        var effective = _config.GetEffectiveBlockSet(_logger);
        _monitor = new ProcessMonitor(effective.ProcessNames, config.CheckIntervalMs, _logger);
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
        var canceled = _lockManager.CancelLock();
        if (canceled)
        {
            _lastLockSweepKey = null;
            _logger.LogInformation(EventIds.LockCleared, "Lock canceled by user.");
        }

        return canceled;
    }

    public LockState GetStatus() => _lockManager.GetStatus();

    public LockState ApplyServiceLock(LockType type, DateTimeOffset startedAtUtc, DateTimeOffset expiresAtUtc)
    {
        var key = CreateLockKey(type, startedAtUtc, expiresAtUtc);
        if (string.Equals(_appliedLockKey, key, StringComparison.Ordinal))
        {
            return _lockManager.GetStatus();
        }

        _appliedLockKey = key;
        var state = _lockManager.ApplyExternalLock(startedAtUtc.ToLocalTime(), expiresAtUtc.ToLocalTime(), type);
        return OnLockActivated(state, "service-sync");
    }

    public void ClearServiceLock()
    {
        _appliedLockKey = null;
        _lastLockSweepKey = null;
        _lockManager.ForceClearLock();
        _logger.LogInformation(EventIds.LockCleared, "Lock cleared via service sync.");
    }

    public void UpdateConfig(BlockerConfig config)
    {
        config.Normalize();
        _config = config;
        var effective = _config.GetEffectiveBlockSet(_logger);
        _monitor.UpdateTargets(effective.ProcessNames);
        _monitor.UpdateCheckInterval(config.CheckIntervalMs);
    }

    private void OnProcessDetected(object? sender, ProcessDetectedEventArgs e)
    {
        if (!_lockManager.IsLockEnforced())
        {
            return;
        }

        _logger.LogInformation(EventIds.ProcessDetected, "Detected blocked process {ProcessName} (PID {Pid})", e.ProcessName, e.ProcessId);
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

    private ProcessTerminationSummary TerminateExistingBlockedProcesses()
    {
        if (_config == null)
        {
            return ProcessTerminationSummary.Empty;
        }

        var effective = _config.GetEffectiveBlockSet(_logger);
        var targets = effective.ProcessNames.ToList();
        if (targets.Count == 0)
        {
            return ProcessTerminationSummary.Empty;
        }

        _logger.LogInformation(EventIds.LockStart, "Pre-existing scan. Selected={Selected} EffectiveApps={EffectiveApps} BlockedProcessesCount={BlockedCount} Processes={Processes}", FormatForLog(_config.SelectedAppKeys), FormatForLog(effective.AppKeys), targets.Count, FormatForLog(targets));
        var summary = ProcessTerminator.TerminateRunningBlockedProcesses(targets, HandleProcessTermination, ProcessTerminationOptions.Default, _logger);
        _logger.LogInformation(EventIds.LockStart, "Pre-existing scan complete. {Summary}", summary.SummaryMessage);
        return summary;
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
            var effective = _config.GetEffectiveBlockSet(_logger);
            var lockKey = CreateLockKey(state.Type, state.StartTime, state.EndTime);
            if (string.Equals(_lastLockSweepKey, lockKey, StringComparison.Ordinal))
            {
                System.Diagnostics.Debug.WriteLine($"[LockStart] Source={source}, lock already enforced (key={lockKey}); skipping pre-existing sweep.");
                return state;
            }

            _lastLockSweepKey = lockKey;
            _logger.LogInformation(EventIds.LockStart, "Lock start enforcement. Source={Source} LockKey={LockKey} LockType={LockType} DurationSeconds={Duration} SelectedApps={Selected} EffectiveApps={EffectiveApps} EffectiveProcessCount={ProcessCount} Processes={Processes}", source, lockKey, state.Type, (state.EndTime - state.StartTime).TotalSeconds, FormatForLog(_config.SelectedAppKeys), FormatForLog(effective.AppKeys), effective.ProcessNames.Count, FormatForLog(effective.ProcessNames));
            var summary = TerminateExistingBlockedProcesses();
            PreExistingProcessesTerminated?.Invoke(this, new PreExistingProcessesTerminatedEventArgs(
                state.Type,
                summary.TerminatedCount,
                summary.TerminatedProcessNames,
                summary.FailedProcessNames,
                summary.SummaryMessage));
            _logger.LogInformation(EventIds.LockStart, "Lock start sweep result. Source={Source} {Summary}", source, summary.SummaryMessage);
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

    private static string CreateLockKey(LockType type, DateTimeOffset startUtc, DateTimeOffset endUtc) =>
        $"{type}:{startUtc:O}:{endUtc:O}";

    private static string FormatForLog(IEnumerable<string> items, int maxItems = 10)
    {
        var list = items.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct(StringComparer.OrdinalIgnoreCase).Take(maxItems + 1).ToList();
        if (list.Count <= maxItems)
        {
            return string.Join(", ", list);
        }

        var head = list.Take(maxItems);
        return $"{string.Join(", ", head)} (+{list.Count - maxItems} more)";
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
    public PreExistingProcessesTerminatedEventArgs(
        LockType type,
        int terminatedCount,
        IReadOnlyCollection<string> terminatedProcessNames,
        IReadOnlyCollection<string> failedProcessNames,
        string summaryMessage)
    {
        LockType = type;
        TerminatedCount = terminatedCount;
        TerminatedProcessNames = terminatedProcessNames;
        FailedProcessNames = failedProcessNames;
        SummaryMessage = summaryMessage;
    }

    public LockType LockType { get; }

    public int TerminatedCount { get; }

    public IReadOnlyCollection<string> TerminatedProcessNames { get; }

    public IReadOnlyCollection<string> FailedProcessNames { get; }

    public string SummaryMessage { get; }
}
