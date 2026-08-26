using System.Text.Json;
using BluetoothMonitor.App.Models;

namespace BluetoothMonitor.App.Services.Test;

public sealed class E2ENotificationService : INotificationService
{
    private static readonly object Sync = new();

    public void ShowLowBattery(string deviceName, byte level, NotificationStyle style)
    {
        if (style == NotificationStyle.Silent)
            return;

        Write("Low battery", $"{deviceName} is at {level}%. Charge soon to avoid disconnection.");
    }

    public void ShowCriticalBattery(string deviceName, byte level)
    {
        Write(
            "Critical battery",
            $"{deviceName} is at {level}%. Charge now to avoid disconnection."
        );
    }

    private static void Write(string title, string message)
    {
        var directory = E2ETestHost.SettingsDirectory;
        if (string.IsNullOrWhiteSpace(directory))
            return;

        Directory.CreateDirectory(directory);
        var entry = JsonSerializer.Serialize(new { title, message });
        lock (Sync)
        {
            File.AppendAllText(
                Path.Combine(directory, "notifications.jsonl"),
                entry + Environment.NewLine
            );
        }
    }
}
