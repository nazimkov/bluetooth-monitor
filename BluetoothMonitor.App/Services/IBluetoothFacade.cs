using System.Collections.Generic;
using System.Threading.Tasks;
using BluetoothMonitor.App.Models;

namespace BluetoothMonitor.App.Services;

public record BluetoothDeviceInfo(
    string Id,
    string Name,
    DeviceKind Kind,
    string? MacAddress,
    bool IsConnected
);

public interface IBluetoothFacade
{
    Task<IReadOnlyList<BluetoothDeviceInfo>> ListDevicesAsync();
    Task<string?> FindDeviceIdAsync(string deviceName);
    Task<byte?> GetBatteryLevelAsync(string deviceId);
    void InvalidateCache();
}
