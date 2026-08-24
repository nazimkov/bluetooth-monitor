using BluetoothMonitor.App.Models;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace BluetoothMonitor.App.Services;

public sealed class AppNotificationService : INotificationService
{
    public void ShowLowBattery(string deviceName, byte level, NotificationStyle style)
    {
        if (style == NotificationStyle.Silent)
            return;

        var builder = new AppNotificationBuilder()
            .AddText("Low battery")
            .AddText($"{deviceName} is at {level}%. Charge soon to avoid disconnection.")
            .AddArgument("action", "open")
            .AddButton(new AppNotificationButton("Dismiss").AddArgument("action", "dismiss"))
            .AddButton(new AppNotificationButton("Snooze 10m").AddArgument("action", "snooze"));

        if (style == NotificationStyle.BannerOnly || style == NotificationStyle.SoundOnly)
        {
            builder.MuteAudio();
        }

        AppNotificationManager.Default.Show(builder.BuildNotification());
    }

    public void ShowCriticalBattery(string deviceName, byte level)
    {
        var notification = new AppNotificationBuilder()
            .AddText("Critical battery")
            .AddText($"{deviceName} is at {level}%. Charge now to avoid disconnection.")
            .AddArgument("action", "open")
            .AddButton(new AppNotificationButton("Dismiss").AddArgument("action", "dismiss"))
            .BuildNotification();
        AppNotificationManager.Default.Show(notification);
    }
}
