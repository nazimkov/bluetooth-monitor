using Windows.Devices.Enumeration;

namespace BluetoothMonitor.Core
{
    public interface IBluetoothDevices
    {
        Task<IReadOnlyList<DeviceInformation>> ListDevicesAsync();
        Task<string?> FindDeviceIdAsync(string deviceName);
        Task<DeviceBatteryLevel> CheckBatteryLevelAsync(string deviceId);
    }
}
