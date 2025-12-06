using System;
using System.Windows;
using Cooldown.Desktop.Services;
using Cooldown.Desktop.IPC;
using Cooldown.Desktop.ViewModels;

namespace Cooldown.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ToastNotificationService _toastService;

    public MainWindow()
    {
        InitializeComponent();
        var configService = new BlockerConfigService();
        var engineHost = new BlockerEngineHost();
        var ipcClient = new NamedPipeClient();
        _viewModel = new MainViewModel(configService, engineHost, ipcClient, Dispatcher);
        DataContext = _viewModel;
        _toastService = new ToastNotificationService();
        _viewModel.ToastRequested += OnToastRequested;
        Loaded += OnLoadedAsync;
        Closed += OnClosedAsync;
    }

    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private async void OnClosedAsync(object? sender, EventArgs e)
    {
        _viewModel.ToastRequested -= OnToastRequested;
        _toastService.Dispose();
        await _viewModel.DisposeAsync();
    }

    private void OnToastRequested(object? sender, ToastNotificationEventArgs e)
    {
        _toastService.Show(e.Title, e.Message);
    }
}
