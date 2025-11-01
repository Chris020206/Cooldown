using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using Cooldown.Blocker.Core;
using Cooldown.Desktop.Commands;
using Cooldown.Desktop.Services;

namespace Cooldown.Desktop.ViewModels;

public class MainViewModel : ObservableObject, IAsyncDisposable
{
    private const int ActivityLogLimit = 50;
    private readonly BlockerConfigService _configService;
    private readonly BlockerEngineHost _engineHost;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _timer;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private BlockerConfig? _config;
    private LockState? _activeLock;
    private int _selectedDuration;
    private int _customDurationMinutes;
    private LockType _selectedLockType = LockType.Soft;
    private string _newAppName = string.Empty;
    private string _statusMessage = "Loading...";
    private string? _errorMessage;
    private bool _isEngineRunning;

    public MainViewModel(BlockerConfigService configService, BlockerEngineHost engineHost, Dispatcher dispatcher)
    {
        _configService = configService;
        _engineHost = engineHost;
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
        CancelLockCommand = new RelayCommand(_ => CancelLock(), _ => LockStatus.CanCancel);
        AddBlockedAppCommand = new AsyncRelayCommand(_ => AddBlockedAppAsync());
        RemoveBlockedAppCommand = new AsyncRelayCommand(app => RemoveBlockedAppAsync(app as BlockedAppViewModel));
        SaveBlockedAppsCommand = new AsyncRelayCommand(_ => SaveConfigurationAsync());

        LockStatus.PropertyChanged += (_, _) => CancelLockCommand.RaiseCanExecuteChanged();
    }

    public ObservableCollection<int> PresetDurations { get; }

    public IEnumerable<LockType> LockTypes { get; } = Enum.GetValues<LockType>();

    public LockStatusViewModel LockStatus { get; }

    public ObservableCollection<BlockedAppViewModel> BlockedApps { get; }

    public ObservableCollection<ProcessEventViewModel> ActivityLog { get; }

    public AsyncRelayCommand CreateLockCommand { get; }

    public RelayCommand CancelLockCommand { get; }

    public AsyncRelayCommand AddBlockedAppCommand { get; }

    public AsyncRelayCommand RemoveBlockedAppCommand { get; }

    public AsyncRelayCommand SaveBlockedAppsCommand { get; }

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

        await _engineHost.StartAsync(_config);
        IsEngineRunning = true;

        UpdateLockState(_engineHost.GetStatus());
        StatusMessage = "Ready";
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

        if (!IsEngineRunning)
        {
            ErrorMessage = "Engine not running";
            return;
        }

        var minutes = CustomDurationMinutes > 0 ? CustomDurationMinutes : SelectedDuration;
        if (minutes <= 0)
        {
            ErrorMessage = "Choose a lock duration greater than zero.";
            return;
        }

        var state = _engineHost.CreateLock(minutes, SelectedLockType);
        UpdateLockState(state);
        StatusMessage = $"{SelectedLockType} lock created for {minutes} minutes";
        await Task.CompletedTask;
    }

    private void CancelLock()
    {
        ErrorMessage = null;

        if (!IsEngineRunning)
        {
            return;
        }

        if (_engineHost.CancelLock())
        {
            StatusMessage = "Lock canceled";
            UpdateLockState(_engineHost.GetStatus());
        }
        else
        {
            ErrorMessage = "Hard locks cannot be canceled.";
            StatusMessage = "Unable to cancel lock";
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

    private async void OnBlockedAppPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        try
        {
            await SaveConfigurationAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save changes: {ex.Message}";
        }
    }

    private void InsertBlockedApp(BlockedAppViewModel app)
    {
        var index = 0;
        while (index < BlockedApps.Count && string.Compare(BlockedApps[index].Name, app.Name, StringComparison.OrdinalIgnoreCase) < 0)
        {
            index++;
        }

        BlockedApps.Insert(index, app);
    }

    private void OnLockStateChanged(object? sender, LockStateChangedEventArgs e)
    {
        _dispatcher.Invoke(() => UpdateLockState(e.State));
    }

    private void UpdateLockState(LockState state)
    {
        if (!state.IsActive)
        {
            _activeLock = null;
            LockStatus.IsActive = false;
            LockStatus.LockType = "None";
            LockStatus.Remaining = "--";
            LockStatus.EndsAt = "--";
            LockStatus.CanCancel = false;
            _timer.Stop();
            return;
        }

        _activeLock = state;
        LockStatus.IsActive = true;
        LockStatus.LockType = state.Type.ToString();
        LockStatus.CanCancel = state.Type == LockType.Soft;
        LockStatus.EndsAt = state.EndTime.ToLocalTime().ToString("t");
        UpdateRemainingTime();

        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }
    }

    private void UpdateRemainingTime()
    {
        if (_activeLock is { IsActive: true } state)
        {
            var remaining = state.EndTime - DateTimeOffset.Now;
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
        foreach (var app in BlockedApps)
        {
            app.PropertyChanged -= OnBlockedAppPropertyChanged;
        }

        await _engineHost.DisposeAsync();
        _saveLock.Dispose();
    }
}
