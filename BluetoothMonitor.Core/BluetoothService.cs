using BluetoothMonitor.Core.Devices;
using Windows.Devices.Enumeration;

namespace BluetoothMonitor.Core
{
    internal class BluetoothService
    {
        private readonly IBluetoothDevices _classicDevices = new BluetoothClassicDevices();
        private readonly IBluetoothDevices _leDevices = new BluetoothLEDevices();

        public BluetoothService(IBluetoothDevices classicDevices, IBluetoothDevices leDevices)
        {
            _classicDevices = classicDevices;
            _leDevices = leDevices;
        }
        public async Task<IReadOnlyList<DeviceInformation>> ListDevicesAsync()
        {
            var classicDevices = await _classicDevices.ListDevicesAsync();
            var leDevices = await _leDevices.ListDevicesAsync();

            return classicDevices
                .Concat(leDevices)
                .GroupBy(d => d.Id)
                .Select(g => g.First())
                .ToList();
        }
    }

}
