using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace BluetoothMonitor.Core
{
    public interface IBluetoothService
    {
        Task<IReadOnlyList<DeviceInformation>> ListDevicesAsync();

        Task<string?> FindDeviceIdAsync(string deviceName);
        Task<DeviceBatteryLevel> CheckBatteryLevelAsync(string deviceId);
    }
}
