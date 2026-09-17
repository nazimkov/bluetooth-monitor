using BluetoothMonitor.App.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace BluetoothMonitor.App.Services;

public sealed class AppNotificationService : INotificationService
{
    private readonly ILogger<AppNotificationService> _logger;

    public AppNotificationService(ILogger<AppNotificationService> logger) => _logger = logger;

    public void ShowLowBattery(string deviceName, byte level, NotificationStyle style)
    {
        if (style == NotificationStyle.Silent)
            return;

        var builder = new AppNotificationBuilder()
            .AddText(AppResources.Get("Notification.LowBattery"))
            .AddText(
                string.Format(AppResources.Get("Notification.LowBatteryDetails"), deviceName, level)
            )
            .AddArgument("action", "open")
            .AddButton(
                new AppNotificationButton(AppResources.Get("Notification.Dismiss")).AddArgument(
                    "action",
                    "dismiss"
                )
            )
            .AddButton(
                new AppNotificationButton(AppResources.Get("Notification.Snooze10m")).AddArgument(
                    "action",
                    "snooze"
                )
            );

        if (style == NotificationStyle.BannerOnly || style == NotificationStyle.SoundOnly)
        {
            builder.MuteAudio();
        }

        AppNotificationManager.Default.Show(builder.BuildNotification());
        _logger.LogInformation("Low battery notification shown at {BatteryLevel} percent", level);
    }

    public void ShowCriticalBattery(string deviceName, byte level)
    {
        var notification = new AppNotificationBuilder()
            .AddText(AppResources.Get("Notification.CriticalBattery"))
            .AddText(
                string.Format(
                    AppResources.Get("Notification.CriticalBatteryDetails"),
                    deviceName,
                    level
                )
            )
            .AddArgument("action", "open")
            .AddButton(
                new AppNotificationButton(AppResources.Get("Notification.Dismiss")).AddArgument(
                    "action",
                    "dismiss"
                )
            )
            .BuildNotification();
        AppNotificationManager.Default.Show(notification);
        _logger.LogInformation(
            "Critical battery notification shown at {BatteryLevel} percent",
            level
        );
    }
}
