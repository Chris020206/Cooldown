namespace Cooldown.Desktop.ViewModels;

public class BlockedAppViewModel : ObservableObject
{
    private bool _enabled;
    private string _name = string.Empty;

    public BlockedAppViewModel(string name, bool enabled)
    {
        _name = name;
        _enabled = enabled;
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }
}
