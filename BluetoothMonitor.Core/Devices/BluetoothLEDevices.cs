using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace BluetoothMonitor.Core.Devices
{
    public sealed class BluetoothLEDevices : IBluetoothDevices
    {
        public async Task<IReadOnlyList<DeviceInformation>> ListDevicesAsync()
        {
            var selector = BluetoothLEDevice.GetDeviceSelector();
            var devices = await DeviceInformation.FindAllAsync(selector);
            return devices;
        }

        public async Task<string?> FindDeviceIdAsync(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                throw new ArgumentException("Device name cannot be null or empty.", nameof(deviceName));
            }

            var selector = BluetoothLEDevice.GetDeviceSelectorFromDeviceName(deviceName);
            var devices = await DeviceInformation.FindAllAsync(selector);
            if (devices is null || devices.Count == 0)
            {
                return null;
            }

            foreach (var device in devices)
            {
                if (device?.Name?.Equals(deviceName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return device.Id;
                }
            }

            return null;
        }

        public async Task<DeviceBatteryLevel> CheckBatteryLevelAsync(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentException("Device ID cannot be null or empty.", nameof(deviceId));
            }

            try
            {
                using BluetoothLEDevice? device = await BluetoothLEDevice.FromIdAsync(deviceId);
                if (device is null)
                {
                    throw new BluetoothException($"Device with ID {deviceId} could not be opened.");
                }

                var servicesResult = await device.GetGattServicesForUuidAsync(GattServiceUuids.Battery);
                if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
                {
                    throw new BluetoothException($"Battery service not available for device {deviceId}. Status: {servicesResult.Status}.");
                }

                using var batteryService = servicesResult.Services[0];

                var characteristicsResult = await batteryService.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel);
                if (characteristicsResult.Status != GattCommunicationStatus.Success || characteristicsResult.Characteristics.Count == 0)
                {
                    throw new BluetoothException($"Battery characteristic not available for device {deviceId}. Status: {characteristicsResult.Status}.");
                }

                var characteristic = characteristicsResult.Characteristics[0];
                var readResult = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (readResult.Status != GattCommunicationStatus.Success || readResult.Value is null || readResult.Value.Length == 0)
                {
                    throw new BluetoothException($"Unable to read battery level for device {deviceId}. Status: {readResult.Status}.");
                }

                var reader = DataReader.FromBuffer(readResult.Value);
                var batteryLevel = reader.ReadByte();
                return new DeviceBatteryLevel(deviceId, batteryLevel);
            }
            catch (BluetoothException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BluetoothException($"Failed to read battery level from device {deviceId}.", ex);
            }
        }
    }
}
