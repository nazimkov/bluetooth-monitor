using BluetoothMonitor.Core.Utils;
using System.Runtime.InteropServices;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace BluetoothMonitor.Core.Devices
{
    internal class BluetoothClassicDevices : IBluetoothService
    {
        private static readonly DEVPROPKEY DEVPKEY_DEVICE_AEP_ID_GUID = new()
        {
            fmtid = new Guid("3B2CE006-5E61-4FDE-BAB8-9B8AAC9B26DF"),
            pid = 8
        };
        private static readonly DEVPROPKEY DEVPKEY_DEVICE_BATTERY_GUID = new ()
        {
            fmtid = new Guid("104EA319-6EE2-4701-BD47-8DDBF425BBE5"),
            pid = 2
        };

        public Task<DeviceBatteryLevel> CheckBatteryLevelAsync(string deviceId)
        {
            if (TryGetBatteryLevel(deviceId, out var device))
            { 
                return Task.FromResult(new DeviceBatteryLevel(
                   device.Id,
                   device.Charge
                ));
            }
            throw new BluetoothException($"Device with ID {deviceId} not found or does not report battery level.");
        }

        public async Task<string?> FindDeviceIdAsync(string deviceName)
        {
            var aqsFilter = BluetoothDevice.GetDeviceSelectorFromDeviceName(deviceName);
            var devices = await DeviceInformation.FindAllAsync(aqsFilter);
            if (devices is null) return null;

            foreach (var device in devices)
            {
                if (device?.Name == deviceName) return device.Id;

            }
            return null;
        }

        public IReadOnlyDictionary<string, ClassicDeviceBatteryLevel> GetAll()
        {
            return GetAllInternal(deviceId: null);
        }

        public async Task<IReadOnlyList<DeviceInformation>> ListDevicesAsync()
        {
            var aqsFilter = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
            var devices = await DeviceInformation.FindAllAsync(aqsFilter);
            return devices;
        }

        public bool TryGetBatteryLevel(string deviceId, out ClassicDeviceBatteryLevel device)
        {
            if (GetAllInternal(deviceId).TryGetValue(deviceId, out var innerDevice))
            {
                device = innerDevice;
                return true;
            }
            device = new(deviceId, 0);
            return false;
        }

        private IReadOnlyDictionary<string, ClassicDeviceBatteryLevel> GetAllInternal(string? deviceId = null)
        {
            Dictionary<string, ClassicDeviceBatteryLevel> allInternal = new();
            var deviceInfoPtr = IntPtr.Zero;
            try
            {
                deviceInfoPtr = SetupAPI.SetupDiGetClassDevs(IntPtr.Zero, null, IntPtr.Zero, DeviceFiter.AllClasses);
                var spDevinfoData = new SP_DEVINFO_DATA();
                var memberIndex = 0;
                spDevinfoData.cbSize = Marshal.SizeOf<SP_DEVINFO_DATA>();
                while (SetupAPI.SetupDiEnumDeviceInfo(deviceInfoPtr, memberIndex++, ref spDevinfoData))
                {
                    try
                    {
                        var deviceIdProp = SetupAPI.GetStringProperty(deviceInfoPtr, ref spDevinfoData, DEVPKEY_DEVICE_AEP_ID_GUID);
                        if (string.IsNullOrEmpty(deviceIdProp) || allInternal.ContainsKey(deviceIdProp))
                        {
                            continue;
                        }
                        var batteryLevelProp = SetupAPI.GetByteProperty(deviceInfoPtr, ref spDevinfoData, DEVPKEY_DEVICE_BATTERY_GUID);
                        if (!batteryLevelProp.HasValue || batteryLevelProp.Value < 0)
                        {
                            continue;
                        }
                        ClassicDeviceBatteryLevel systemChargableDevice = new(deviceIdProp, batteryLevelProp.Value);

                        if (deviceId == null)
                            allInternal.Add(deviceIdProp, systemChargableDevice);
                        else if (deviceIdProp == deviceId)
                        {
                            allInternal.Add(deviceIdProp, systemChargableDevice);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Ignore individual device errors
                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (deviceInfoPtr != IntPtr.Zero)
                    SetupAPI.SetupDiDestroyDeviceInfoList(deviceInfoPtr);
            }
            return allInternal;
        }
    }

    internal record ClassicDeviceBatteryLevel (string Id, byte Charge);
}
