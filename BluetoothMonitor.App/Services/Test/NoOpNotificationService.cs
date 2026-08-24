using BluetoothMonitor.App.Models;

namespace BluetoothMonitor.App.Services.Test;

public sealed class NoOpNotificationService : INotificationService
{
    public void ShowLowBattery(string deviceName, byte level, NotificationStyle style) { }

    public void ShowCriticalBattery(string deviceName, byte level) { }
}
