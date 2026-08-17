using BluetoothMonitor.App.Models;

namespace BluetoothMonitor.App.Services;

public interface INotificationService
{
    void ShowLowBattery(string deviceName, byte level, NotificationStyle style);
    void ShowCriticalBattery(string deviceName, byte level);
}
