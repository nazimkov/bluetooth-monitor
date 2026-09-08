using System.Runtime.InteropServices;
using BluetoothMonitor.Core.Utils;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace BluetoothMonitor.Core.Devices
{
    public sealed class BluetoothClassicDevices : IBluetoothDevices
    {
        private static readonly string[] RequestedProperties =
        {
            "System.Devices.Aep.DeviceAddress",
            "System.Devices.Aep.IsConnected",
            "System.Devices.Aep.IsPaired",
        };

        private static readonly DEVPROPKEY DEVPKEY_DEVICE_AEP_ID_GUID = new()
        {
            fmtid = new Guid("3B2CE006-5E61-4FDE-BAB8-9B8AAC9B26DF"),
            pid = 8,
        };
        private static readonly DEVPROPKEY DEVPKEY_DEVICE_BATTERY_GUID = new()
        {
            fmtid = new Guid("104EA319-6EE2-4701-BD47-8DDBF425BBE5"),
            pid = 2,
        };

        public Task<DeviceBatteryLevel> CheckBatteryLevelAsync(string deviceId)
        {
            if (TryGetBatteryLevel(deviceId, out var device))
            {
                return Task.FromResult(new DeviceBatteryLevel(device.Id, device.Charge));
            }
            throw new BluetoothException(
                $"Device with ID {deviceId} not found or does not report battery level."
            );
        }

        public async Task<string?> FindDeviceIdAsync(string deviceName)
        {
            var aqsFilter = BluetoothDevice.GetDeviceSelectorFromDeviceName(deviceName);
            var devices = await DeviceInformation.FindAllAsync(aqsFilter, RequestedProperties);
            if (devices is null)
                return null;

            foreach (var device in devices)
            {
                if (device?.Name == deviceName)
                    return device.Id;
            }
            return null;
        }

        public async Task<IReadOnlyList<DeviceInformation>> ListDevicesAsync()
        {
            var aqsFilter = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
            var devices = await DeviceInformation.FindAllAsync(aqsFilter, RequestedProperties);
            return devices;
        }

        private bool TryGetBatteryLevel(string deviceId, out ClassicDeviceBatteryLevel device)
        {
            foreach (var candidate in EnumerateClassicDeviceBatteryLevels())
            {
                if (candidate.Id == deviceId)
                {
                    device = candidate;
                    return true;
                }
            }
            device = new(deviceId, 0);
            return false;
        }

        private IEnumerable<ClassicDeviceBatteryLevel> EnumerateClassicDeviceBatteryLevels()
        {
            var deviceInfoPtr = IntPtr.Zero;
            HashSet<string> seenDeviceIds = new();
            try
            {
                deviceInfoPtr = SetupAPI.SetupDiGetClassDevs(
                    IntPtr.Zero,
                    null,
                    IntPtr.Zero,
                    DeviceFiter.AllClasses
                );
                var spDevinfoData = new SP_DEVINFO_DATA
                {
                    cbSize = Marshal.SizeOf<SP_DEVINFO_DATA>(),
                };
                var memberIndex = 0;
                while (
                    SetupAPI.SetupDiEnumDeviceInfo(deviceInfoPtr, memberIndex++, ref spDevinfoData)
                )
                {
                    var device = CreateBatteryLevel(deviceInfoPtr, ref spDevinfoData);
                    if (device is not null && seenDeviceIds.Add(device.Id))
                    {
                        yield return device;
                    }
                }
            }
            finally
            {
                if (deviceInfoPtr != IntPtr.Zero)
                {
                    SetupAPI.SetupDiDestroyDeviceInfoList(deviceInfoPtr);
                }
            }
        }

        private ClassicDeviceBatteryLevel? CreateBatteryLevel(
            IntPtr deviceInfoPtr,
            ref SP_DEVINFO_DATA spDevinfoData
        )
        {
            try
            {
                var deviceIdProp = SetupAPI.GetStringProperty(
                    deviceInfoPtr,
                    ref spDevinfoData,
                    DEVPKEY_DEVICE_AEP_ID_GUID
                );
                if (string.IsNullOrEmpty(deviceIdProp))
                {
                    return null;
                }

                var batteryLevelProp = SetupAPI.GetByteProperty(
                    deviceInfoPtr,
                    ref spDevinfoData,
                    DEVPKEY_DEVICE_BATTERY_GUID
                );
                if (!batteryLevelProp.HasValue || batteryLevelProp.Value < 0)
                {
                    return null;
                }

                return new ClassicDeviceBatteryLevel(deviceIdProp, batteryLevelProp.Value);
            }
            catch
            {
                return null;
            }
        }
    }

    internal record ClassicDeviceBatteryLevel(string Id, byte Charge);
}
