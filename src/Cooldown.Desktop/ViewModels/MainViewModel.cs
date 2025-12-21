using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Cooldown.Blocker.Core;
using Cooldown.Desktop.Commands;
using Cooldown.Desktop.IPC;
using Cooldown.Desktop.Services;

namespace Cooldown.Desktop.ViewModels;

public class MainViewModel : ObservableObject, IAsyncDisposable
{
    private const int ActivityLogLimit = 50;
    private readonly BlockerConfigService _configService;
    private readonly BlockerEngineHost _engineHost;
    private readonly INamedPipeClient _ipcClient;
    private readonly ILockIpcClient _lockIpcClient;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _timer;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly CancellationTokenSource _ipcListeningCts = new();
    private BlockerConfig? _config;
    private LockState? _activeLock;
    private int _selectedDuration;
    private int _customDurationMinutes;
    private LockType _selectedLockType = LockType.Soft;
    private string _newAppName = string.Empty;
    private string _statusMessage = "Loading...";
    private string _serviceStatus = "Service not checked.";
    private string? _errorMessage;
    private bool _isEngineRunning;

    public event EventHandler<ToastNotificationEventArgs>? ToastRequested;

    public MainViewModel(BlockerConfigService configService, BlockerEngineHost engineHost, INamedPipeClient ipcClient, Dispatcher dispatcher)
    {
        _configService = configService;
        _engineHost = engineHost;
        _ipcClient = ipcClient;
        _lockIpcClient = new LockIpcClient(_ipcClient);
        _dispatcher = dispatcher;
        PresetDurations = new ObservableCollection<int>(new[] { 5, 15, 30, 60, 120, 240 });
        _selectedDuration = PresetDurations.First();

        LockStatus = new LockStatusViewModel();
        BlockedApps = new ObservableCollection<BlockedAppViewModel>();
        ActivityLog = new ObservableCollection<ProcessEventViewModel>();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) => UpdateRemainingTime();

        CreateLockCommand = new AsyncRelayCommand(_ => CreateLockAsync(), _ => IsEngineRunning);
        CancelLockCommand = new AsyncRelayCommand(_ => CancelLockAsync(), _ => LockStatus.CanCancel);
        AddBlockedAppCommand = new AsyncRelayCommand(_ => AddBlockedAppAsync());
        RemoveBlockedAppCommand = new AsyncRelayCommand(app => RemoveBlockedAppAsync(app as BlockedAppViewModel));
        SaveBlockedAppsCommand = new AsyncRelayCommand(_ => SaveConfigurationAsync());
        PingServiceCommand = new AsyncRelayCommand(_ => PingServiceAsync());

        LockStatus.PropertyChanged += (_, _) => CancelLockCommand.RaiseCanExecuteChanged();
    }

    public ObservableCollection<int> PresetDurations { get; }

    public IEnumerable<LockType> LockTypes { get; } = Enum.GetValues<LockType>();

    public LockStatusViewModel LockStatus { get; }

    public ObservableCollection<BlockedAppViewModel> BlockedApps { get; }

    public ObservableCollection<ProcessEventViewModel> ActivityLog { get; }

    public AsyncRelayCommand CreateLockCommand { get; }

    public AsyncRelayCommand CancelLockCommand { get; }

    public AsyncRelayCommand AddBlockedAppCommand { get; }

    public AsyncRelayCommand RemoveBlockedAppCommand { get; }

    public AsyncRelayCommand SaveBlockedAppsCommand { get; }

    public AsyncRelayCommand PingServiceCommand { get; }

    public int SelectedDuration
    {
        get => _selectedDuration;
        set => SetProperty(ref _selectedDuration, value);
    }

    public int CustomDurationMinutes
    {
        get => _customDurationMinutes;
        set => SetProperty(ref _customDurationMinutes, value);
    }

    public LockType SelectedLockType
    {
        get => _selectedLockType;
        set => SetProperty(ref _selectedLockType, value);
    }

    public string NewAppName
    {
        get => _newAppName;
        set => SetProperty(ref _newAppName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string ServiceStatus
    {
        get => _serviceStatus;
        set => SetProperty(ref _serviceStatus, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsEngineRunning
    {
        get => _isEngineRunning;
        private set
        {
            if (_isEngineRunning != value)
            {
                _isEngineRunning = value;
                OnPropertyChanged();
                CreateLockCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public async Task InitializeAsync()
    {
        StatusMessage = "Loading configuration";
        _config = await _configService.LoadAsync();
        PopulateBlockedApps(_config);

        _engineHost.LockStateChanged += OnLockStateChanged;
        _engineHost.ProcessBlocked += OnProcessBlocked;
        _engineHost.PreExistingProcessesTerminated += OnPreExistingProcessesTerminated;
        _lockIpcClient.LockStateChanged += OnServiceLockStateChanged;
        await _lockIpcClient.StartListeningAsync(_ipcListeningCts.Token);

        await _engineHost.StartAsync(_config);
        IsEngineRunning = true;

        await RefreshLockStateAsync();
        StatusMessage = "Ready";

        // Optionally attempt an initial ping to surface service availability.
        await PingServiceAsync();
    }

    private void PopulateBlockedApps(BlockerConfig config)
    {
        BlockedApps.Clear();
        foreach (var app in config.Apps.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
        {
            var vm = new BlockedAppViewModel(app.Name, app.Enabled);
            vm.PropertyChanged += OnBlockedAppPropertyChanged;
            BlockedApps.Add(vm);
        }
    }

    private async Task CreateLockAsync()
    {
        ErrorMessage = null;

        var minutes = CustomDurationMinutes > 0 ? CustomDurationMinutes : SelectedDuration;
        if (minutes <= 0)
        {
            ErrorMessage = "Choose a lock duration greater than zero.";
            return;
        }

        System.Diagnostics.Debug.WriteLine($"CreateLockAsync invoked. Type={SelectedLockType}, minutes={minutes}");
        var response = await _lockIpcClient.CreateLockAsync(new LockCreateRequest
        {
            Type = SelectedLockType.ToString(),
            DurationSeconds = minutes * 60,
            BlockedApps = BlockedApps.Where(a => a.Enabled).Select(a => a.Name).ToArray()
        });

        if (response.Success && response.Result != null)
        {
            ApplyLockState(response.Result);
            StatusMessage = $"{SelectedLockType} lock created for {minutes} minutes";
            ErrorMessage = null;
            await RefreshLockStateAsync();
        }
        else
        {
            var err = response.Error;
            ErrorMessage = err != null ? $"Service error: {err.Code} - {err.Message}" : "Service unavailable (lock create failed).";
            StatusMessage = "Failed to create lock via service.";
        }
    }

    private void CancelLock()
    {
        ErrorMessage = null;

        _ = CancelLockAsync();
    }

    private async Task CancelLockAsync()
    {
        System.Diagnostics.Debug.WriteLine("CancelLockAsync invoked.");
        var response = await _lockIpcClient.CancelLockAsync(new LockCancelRequest());
        if (response.Success && response.Result != null && response.Result.Canceled)
        {
            StatusMessage = "Lock canceled";
            ErrorMessage = null;
            await RefreshLockStateAsync();
        }
        else
        {
            var err = response.Error;
            ErrorMessage = err != null ? $"Service error: {err.Code} - {err.Message}" : "Unable to cancel lock via service.";
            StatusMessage = "Unable to cancel lock";
            await RefreshLockStateAsync();
        }
    }

    private async Task AddBlockedAppAsync()
    {
        ErrorMessage = null;
        var trimmed = (NewAppName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            ErrorMessage = "Enter a process name.";
            return;
        }

        if (BlockedApps.Any(app => string.Equals(app.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = "Process already listed.";
            return;
        }

        var vm = new BlockedAppViewModel(trimmed, true);
        vm.PropertyChanged += OnBlockedAppPropertyChanged;
        InsertBlockedApp(vm);
        NewAppName = string.Empty;
        await SaveConfigurationAsync();
    }

    private async Task RemoveBlockedAppAsync(BlockedAppViewModel? app)
    {
        if (app == null)
        {
            return;
        }

        app.PropertyChanged -= OnBlockedAppPropertyChanged;
        BlockedApps.Remove(app);
        await SaveConfigurationAsync();
    }

    private async Task RefreshLockStateAsync()
    {
        System.Diagnostics.Debug.WriteLine("RefreshLockStateAsync invoked.");
        var response = await _lockIpcClient.GetLockStateAsync();
        if (response.Success && response.Result != null)
        {
            if (!response.Result.HasActiveLock || response.Result.Lock == null)
            {
                ClearLockDisplay();
                return;
            }

            ApplyLockState(response.Result.Lock);
        }
        else
        {
            var err = response.Error;
            ServiceStatus = err != null ? $"Service error: {err.Code} - {err.Message}" : "Service unavailable (lock state).";
        }
    }

    private async Task SaveConfigurationAsync()
    {
        if (_config == null)
        {
            return;
        }

        await _saveLock.WaitAsync();
        try
        {
            _config.Apps = BlockedApps
                .Select(app => new BlockableApp(app.Name, app.Enabled))
                .ToList();

            await _configService.SaveAsync(_config);

            if (IsEngineRunning)
            {
                _engineHost.UpdateConfiguration(_config);
            }

            StatusMessage = "Changes saved";
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private async Task PingServiceAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("PingServiceAsync invoked.");
            var response = await _ipcClient.SendCommandAsync<object, PingResponsePayload>(
                "Service.Ping",
                new { clientVersion = "0.2.1-desktop" },
                CancellationToken.None);

            if (response.Success && response.Result != null)
            {
                ServiceStatus = $"Service v{response.Result.ServiceVersion}, protocol {response.Result.ProtocolVersion}, uptime {response.Result.UptimeSeconds}s";
            }
            else if (response.Error != null)
            {
                ServiceStatus = $"Service error: {response.Error.Code} - {response.Error.Message}";
            }
            else
            {
                ServiceStatus = "Service ping returned an unknown response.";
            }
        }
        catch (Exception ex)
        {
            ServiceStatus = $"Service unavailable ({ex.Message}).";
        }
    }

    private void ApplyLockState(LockCreateResponse response)
    {
        _activeLock = new LockState
        {
            IsActive = true,
            Type = Enum.TryParse<LockType>(response.Type, true, out var parsed) ? parsed : LockType.Soft,
            StartTime = response.StartedAtUtc,
            EndTime = response.ExpiresAtUtc,
            DurationMinutes = response.DurationSeconds / 60
        };

        LockStatus.IsActive = true;
        LockStatus.LockType = _activeLock.Type.ToString();
        LockStatus.CanCancel = _activeLock.Type == LockType.Soft;
        LockStatus.EndsAt = response.ExpiresAtUtc.ToLocalTime().ToString("t");
        UpdateRemainingTime();
        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }

        // Keep local enforcement in sync with the service lock so processes get terminated.
        _engineHost.ApplyServiceLock(_activeLock.Type, response.StartedAtUtc, response.ExpiresAtUtc);
    }

    private void ApplyLockState(LockStateDto dto)
    {
        _activeLock = new LockState
        {
            IsActive = true,
            Type = Enum.TryParse<LockType>(dto.Type, true, out var parsed) ? parsed : LockType.Soft,
            StartTime = dto.StartedAtUtc,
            EndTime = dto.ExpiresAtUtc,
            DurationMinutes = dto.DurationSeconds / 60
        };

        LockStatus.IsActive = true;
        LockStatus.LockType = _activeLock.Type.ToString();
        LockStatus.CanCancel = _activeLock.Type == LockType.Soft;
        LockStatus.EndsAt = dto.ExpiresAtUtc.ToLocalTime().ToString("t");
        UpdateRemainingTime();
        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }

        // Service-driven lock state updates must also drive local enforcement.
        _engineHost.ApplyServiceLock(_activeLock.Type, dto.StartedAtUtc, dto.ExpiresAtUtc);
    }

    private void ClearLockDisplay()
    {
        _activeLock = null;
        LockStatus.IsActive = false;
        LockStatus.LockType = "None";
        LockStatus.Remaining = "--";
        LockStatus.EndsAt = "--";
        LockStatus.CanCancel = false;
        _timer.Stop();
        _engineHost.ClearServiceLock();
    }

    private void OnBlockedAppPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _ = SaveConfigurationAsync();
    }

    private void InsertBlockedApp(BlockedAppViewModel app)
    {
        var index = 0;
        while (index < BlockedApps.Count &&
               string.Compare(BlockedApps[index].Name, app.Name, StringComparison.OrdinalIgnoreCase) < 0)
        {
            index++;
        }

        BlockedApps.Insert(index, app);
    }

    private void OnLockStateChanged(object? sender, LockStateChangedEventArgs e)
    {
        // Local lock manager tick emits this frequently; avoid IPC spam.
        // UI stays in sync via service push + explicit refresh elsewhere.
    }

    private void OnServiceLockStateChanged(LockStateChangedEventPayload payload)
    {
        _ = _dispatcher.InvokeAsync(() =>
        {
            if (!payload.HasActiveLock || payload.Lock == null)
            {
                ClearLockDisplay();
                StatusMessage = payload.Reason == "Expired" ? "Lock expired" : "No active lock";
                return;
            }

            ApplyLockState(payload.Lock);
            var reason = payload.Reason;
            StatusMessage = reason switch
            {
                "Created" => "Lock created",
                "Canceled" => "Lock canceled",
                "Expired" => "Lock expired",
                _ => "Lock state updated"
            };
        });
    }

    private void UpdateRemainingTime()
    {
        if (_activeLock != null)
        {
            var remaining = _activeLock.EndTime - DateTimeOffset.Now;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            LockStatus.Remaining = $"{(int)remaining.TotalMinutes:D2}m {remaining.Seconds:D2}s";
        }
        else
        {
            LockStatus.Remaining = "--";
            _timer.Stop();
        }
    }

    private void OnProcessBlocked(object? sender, ProcessBlockedEventArgs e)
    {
        _dispatcher.Invoke(() =>
        {
            ActivityLog.Insert(0, new ProcessEventViewModel
            {
                Timestamp = DateTimeOffset.Now,
                ProcessName = e.ProcessName,
                Message = e.Result.Message
            });

            while (ActivityLog.Count > ActivityLogLimit)
            {
                ActivityLog.RemoveAt(ActivityLog.Count - 1);
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        _timer.Stop();
        _engineHost.LockStateChanged -= OnLockStateChanged;
        _engineHost.ProcessBlocked -= OnProcessBlocked;
        _engineHost.PreExistingProcessesTerminated -= OnPreExistingProcessesTerminated;
        _lockIpcClient.LockStateChanged -= OnServiceLockStateChanged;
        _ipcListeningCts.Cancel();
        _ipcListeningCts.Dispose();
        foreach (var app in BlockedApps)
        {
            app.PropertyChanged -= OnBlockedAppPropertyChanged;
        }

        await _engineHost.DisposeAsync();
        _saveLock.Dispose();
        await _ipcClient.DisposeAsync();
    }

    private void OnPreExistingProcessesTerminated(object? sender, PreExistingProcessesTerminatedEventArgs e)
    {
        _dispatcher.Invoke(() =>
        {
            var message = e.SummaryMessage;

            ActivityLog.Insert(0, new ProcessEventViewModel
            {
                Timestamp = DateTimeOffset.Now,
                ProcessName = "Lock Enforcement",
                Message = message
            });

            while (ActivityLog.Count > ActivityLogLimit)
            {
                ActivityLog.RemoveAt(ActivityLog.Count - 1);
            }

            StatusMessage = message;

            if (e.LockType == LockType.Soft && e.TerminatedCount > 0)
            {
                ToastRequested?.Invoke(this, new ToastNotificationEventArgs(
                    "Cooldown.gg",
                    "Blocked apps were closed to enforce the lock."));
            }
        });
    }
}
