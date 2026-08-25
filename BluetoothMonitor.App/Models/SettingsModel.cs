namespace BluetoothMonitor.App.Models;

public class SettingsModel
{
    public string? SelectedDeviceId { get; set; }
    public string DeviceName { get; set; } = "Baseus Bowie D05";
    public int LowBatteryThreshold { get; set; } = 20;
    public bool CriticalAlertUnder5 { get; set; } = true;
    public bool SilenceDuringDnd { get; set; }
    public NotificationStyle NotificationStyle { get; set; } = NotificationStyle.BannerAndSound;
    public string AlertSound { get; set; } = "Gentle";
    public int RefreshIntervalSeconds { get; set; } = 60;
    public bool StartAtSignIn { get; set; }
    public bool KeepRunningInBackground { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.System;

    public SettingsModel Clone() =>
        new()
        {
            SelectedDeviceId = SelectedDeviceId,
            DeviceName = DeviceName,
            LowBatteryThreshold = LowBatteryThreshold,
            CriticalAlertUnder5 = CriticalAlertUnder5,
            SilenceDuringDnd = SilenceDuringDnd,
            NotificationStyle = NotificationStyle,
            AlertSound = AlertSound,
            RefreshIntervalSeconds = RefreshIntervalSeconds,
            StartAtSignIn = StartAtSignIn,
            KeepRunningInBackground = KeepRunningInBackground,
            Theme = Theme,
        };
}
