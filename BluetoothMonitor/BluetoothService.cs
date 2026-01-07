using BluetoothMonitor.Core;
using BluetoothMonitor.Core.Devices;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace BluetoothMonitor;

// https://github.com/microsoft/Windows-universal-samples/blob/main/Samples/BluetoothLE/cs/Scenario1_Discovery.xaml.cs
// https://baydachnyy.com/2017/04/28/uwp-working-with-bluetooth-devices-part-1/
// https://github.com/inthehand/32feet/issues/75


public sealed class BluetoothService
{
    private readonly IBluetoothDevices _classicDevices = new BluetoothClassicDevices();
    private readonly IBluetoothDevices _leDevices = new BluetoothLEDevices();

    public BluetoothService() => InitializeDeviceWatcher();

    private const string BTDeviceFriendlyName = "Baseus Bowie D05";
    private DeviceWatcher? _deviceWatcher;

    private void InitializeDeviceWatcher()
    {
        //string aqsFilter = $"System.Devices.Aep.DeviceContainer.Category:\"{{65A0EBA6-5783-4741-9027-33EFA8D2742C}}\" AND System.Devices.Aep.Bluetooth.Le.IsConnectable:System.StructuredQueryType.Boolean#True AND System.ItemNameDisplay:~\"{BTDeviceFriendlyName}\"";
        //string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

        //_deviceWatcher = DeviceInformation.CreateWatcher(aqsFilter, requestedProperties, DeviceInformationKind.AssociationEndpoint);

        //_deviceWatcher.Added += DeviceWatcher_Added;
        //_deviceWatcher.Updated += DeviceWatcher_Updated;
        //_deviceWatcher.Removed += DeviceWatcher_Removed;

        //_deviceWatcher.Start();
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

    public async Task<string?> FindDeviceIdAsync(string deviceName)
    {
        var leDeviceId = await _leDevices.FindDeviceIdAsync(deviceName);
        if (!string.IsNullOrWhiteSpace(leDeviceId))
        {
            return leDeviceId;
        }

        return await _classicDevices.FindDeviceIdAsync(deviceName);
    }

    private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args) =>
        await CheckBatteryLevelAsync(args.Id);

    private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args) =>
        await CheckBatteryLevelAsync(args.Id);

    private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        // Handle device removal if needed
    }

    public async Task<byte> GetDeviceBatteryLevel(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("Device ID cannot be null or empty.", nameof(deviceId));
        }
        IBluetoothDevices service = _classicDevices;
        var isLeDevice = await IsBluetoothLeDeviceAsync(deviceId);
        if (isLeDevice)
        {
            service = _leDevices;
        }
        try
        {
            var battery = await service.CheckBatteryLevelAsync(deviceId);
            return battery.BatteryLevel;
        }
        catch (BluetoothException ex)
        {
            Console.WriteLine($"Bluetooth error reading battery level for {deviceId}: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error reading battery level for {deviceId}: {ex.Message}");
            throw;
        }
    }



    /*
    private void UpdateBatteryLevel(string batteryLevel)
    {
        if (int.TryParse(batteryLevel, out int level))
        {
            notifyIcon1.Text = $"{BTDeviceFriendlyName} Battery: {level}%";

            // You can add logic here to change the icon based on the battery level
            // For example:
            // if (level <= 20)
            //     notifyIcon1.Icon = new Icon("assets\\low_battery_icon.ico");
            // else
            //     notifyIcon1.Icon = new Icon("assets\\normal_battery_icon.ico");

            if (level <= 20)
            {
                notifyIcon1.ShowBalloonTip(5000, "Low Battery", $"{BTDeviceFriendlyName} battery is low: {level}%", ToolTipIcon.Warning);
            }
        }
        else
        {
            notifyIcon1.Text = $"{BTDeviceFriendlyName} Battery: Unknown";
            MessageBox.Show($"Unexpected battery level: {batteryLevel}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    */

    private async Task CheckBatteryLevelAsync(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        IBluetoothDevices service = _classicDevices;
        var isLeDevice = await IsBluetoothLeDeviceAsync(deviceId);
        if (isLeDevice)
        {
            service = _leDevices;
        }

        try
        {
            var battery = await service.CheckBatteryLevelAsync(deviceId);
            Console.WriteLine($"Battery level for {battery.DeviceId}: {battery.BatteryLevel}%");
        }
        catch (BluetoothException ex)
        {
            Console.WriteLine($"Bluetooth error reading battery level for {deviceId}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error reading battery level for {deviceId}: {ex.Message}");
        }
    }

    private static async Task<bool> IsBluetoothLeDeviceAsync(string deviceId)
    {
        try
        {
            using BluetoothLEDevice? device = await BluetoothLEDevice.FromIdAsync(deviceId);
            return device is not null;
        }
        catch
        {
            return false;
        }
    }
}

