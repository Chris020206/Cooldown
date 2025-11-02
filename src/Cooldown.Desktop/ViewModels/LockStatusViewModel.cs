namespace Cooldown.Desktop.ViewModels;

public class LockStatusViewModel : ObservableObject
{
    private bool _isActive;
    private string _lockType = "Soft";
    private string _remaining = "--";
    private string _endsAt = "--";
    private bool _canCancel;

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string LockType
    {
        get => _lockType;
        set => SetProperty(ref _lockType, value);
    }

    public string Remaining
    {
        get => _remaining;
        set => SetProperty(ref _remaining, value);
    }

    public string EndsAt
    {
        get => _endsAt;
        set => SetProperty(ref _endsAt, value);
    }

    public bool CanCancel
    {
        get => _canCancel;
        set => SetProperty(ref _canCancel, value);
    }
}
