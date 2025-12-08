using Cooldown.Blocker.Core;

namespace Cooldown.Desktop.Services;

public sealed class BlockerEngineHost : IAsyncDisposable
{
    private BlockerEngine? _engine;
    private BlockerConfig? _config;

    public event EventHandler<LockStateChangedEventArgs>? LockStateChanged;
    public event EventHandler<ProcessBlockedEventArgs>? ProcessBlocked;
    public event EventHandler<PreExistingProcessesTerminatedEventArgs>? PreExistingProcessesTerminated;

    public bool IsRunning => _engine != null;

    public async Task StartAsync(BlockerConfig config)
    {
        if (_engine != null)
        {
            return;
        }

        _config = config;
        _engine = new BlockerEngine(config);
        _engine.LockStateChanged += OnLockStateChanged;
        _engine.ProcessBlocked += OnProcessBlocked;
        _engine.PreExistingProcessesTerminated += OnPreExistingProcessesTerminated;
        await _engine.StartAsync();
    }

    public LockState CreateLock(int minutes, LockType type)
    {
        EnsureRunning();
        return _engine!.CreateLock(minutes, type);
    }

    public bool CancelLock()
    {
        EnsureRunning();
        return _engine!.CancelLock();
    }

    public LockState GetStatus()
    {
        EnsureRunning();
        return _engine!.GetStatus();
    }

    public LockState ApplyServiceLock(LockType type, DateTimeOffset startedAtUtc, DateTimeOffset expiresAtUtc)
    {
        EnsureRunning();
        return _engine!.ApplyServiceLock(type, startedAtUtc, expiresAtUtc);
    }

    public void ClearServiceLock()
    {
        EnsureRunning();
        _engine!.ClearServiceLock();
    }

    public void UpdateConfiguration(BlockerConfig config)
    {
        EnsureRunning();
        _config = config;
        _engine!.UpdateConfig(config);
    }

    public async ValueTask DisposeAsync()
    {
        if (_engine == null)
        {
            return;
        }

        _engine.LockStateChanged -= OnLockStateChanged;
        _engine.ProcessBlocked -= OnProcessBlocked;
        _engine.PreExistingProcessesTerminated -= OnPreExistingProcessesTerminated;
        await _engine.DisposeAsync();
        _engine = null;
    }

    private void EnsureRunning()
    {
        if (_engine == null)
        {
            throw new InvalidOperationException("Engine not started.");
        }
    }

    private void OnLockStateChanged(object? sender, LockStateChangedEventArgs e)
    {
        LockStateChanged?.Invoke(this, e);
    }

    private void OnProcessBlocked(object? sender, ProcessBlockedEventArgs e)
    {
        ProcessBlocked?.Invoke(this, e);
    }

    private void OnPreExistingProcessesTerminated(object? sender, PreExistingProcessesTerminatedEventArgs e)
    {
        PreExistingProcessesTerminated?.Invoke(this, e);
    }
}
