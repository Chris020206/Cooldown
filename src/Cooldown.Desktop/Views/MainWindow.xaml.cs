using System;
using System.Windows;
using Cooldown.Desktop.Services;
using Cooldown.Desktop.ViewModels;

namespace Cooldown.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        var configService = new BlockerConfigService();
        var engineHost = new BlockerEngineHost();
        _viewModel = new MainViewModel(configService, engineHost, Dispatcher);
        DataContext = _viewModel;
        Loaded += OnLoadedAsync;
        Closed += OnClosedAsync;
    }

    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private async void OnClosedAsync(object? sender, EventArgs e)
    {
        await _viewModel.DisposeAsync();
    }
}
