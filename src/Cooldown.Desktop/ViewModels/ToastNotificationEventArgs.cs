using System;

namespace Cooldown.Desktop.ViewModels;

public class ToastNotificationEventArgs : EventArgs
{
    public ToastNotificationEventArgs(string title, string message)
    {
        Title = title;
        Message = message;
    }

    public string Title { get; }

    public string Message { get; }
}
