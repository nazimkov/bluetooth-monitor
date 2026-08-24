using System;

namespace BluetoothMonitor.App.Services.Test;

/// <summary>
/// Detects and reads BATTCHECK_E2E_* environment variables used by the E2E harness.
/// </summary>
public static class E2ETestHost
{
    public const string EnvFlag = "BATTCHECK_E2E";
    public const string EnvSettingsDir = "BATTCHECK_E2E_SETTINGS_DIR";
    public const string EnvInstanceKey = "BATTCHECK_E2E_INSTANCE_KEY";
    public const string EnvDevicesJson = "BATTCHECK_E2E_DEVICES_JSON";

    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable(EnvFlag), "1", StringComparison.Ordinal);

    public static string? SettingsDirectory => Environment.GetEnvironmentVariable(EnvSettingsDir);

    public static string InstanceKey =>
        Environment.GetEnvironmentVariable(EnvInstanceKey) is { Length: > 0 } key
            ? key
            : "BluetoothMonitor.App.E2E";

    public static string? DevicesJson => Environment.GetEnvironmentVariable(EnvDevicesJson);
}
