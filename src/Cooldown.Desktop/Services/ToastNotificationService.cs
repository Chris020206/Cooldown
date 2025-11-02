using System;
using System.Drawing;
using System.Windows.Forms;

namespace Cooldown.Desktop.Services;

public sealed class ToastNotificationService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public ToastNotificationService()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Visible = true,
            Text = "Cooldown.gg"
        };
    }

    public void Show(string title, string message, int durationMilliseconds = 3000)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(durationMilliseconds);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
