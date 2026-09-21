using BluetoothMonitor.App.Models;

namespace BluetoothMonitor.App.Services;

public enum BatteryIconState
{
    Critical,
    Low,
    Normal,
    Offline,
}

public readonly record struct BatteryRange(int Start, int End)
{
    public override string ToString() => $"{Start}–{End}%";
}

public static class BatteryRangePolicy
{
    public const int DefaultCriticalMaximum = 5;
    public const int DefaultLowMaximum = 20;

    public static BatteryRange Critical(SettingsModel settings) =>
        new(0, settings.CriticalBatteryMaximum);

    public static BatteryRange Low(SettingsModel settings) =>
        new(settings.CriticalBatteryMaximum + 1, settings.LowBatteryMaximum);

    public static BatteryRange Normal(SettingsModel settings) =>
        new(settings.LowBatteryMaximum + 1, 100);

    public static bool IsCriticalMaximumValid(int value, int lowMaximum) =>
        value is >= 0 and < 99 && value < lowMaximum;

    public static bool IsLowMaximumValid(int value, int criticalMaximum) =>
        value is > 0 and <= 99 && value > criticalMaximum;

    public static BatteryIconState Classify(byte? level, bool connected, SettingsModel settings) =>
        !connected || level is null ? BatteryIconState.Offline
        : level.Value <= settings.CriticalBatteryMaximum ? BatteryIconState.Critical
        : level.Value <= settings.LowBatteryMaximum ? BatteryIconState.Low
        : BatteryIconState.Normal;
}
