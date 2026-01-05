using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace BluetoothMonitor;

// https://github.com/microsoft/Windows-universal-samples/blob/main/Samples/BluetoothLE/cs/Scenario1_Discovery.xaml.cs
// https://baydachnyy.com/2017/04/28/uwp-working-with-bluetooth-devices-part-1/

public sealed class BluetoothService
{

    // public BluetoothService() => InitializeDeviceWatcher();

    private const string BTDeviceFriendlyName = "Baseus Bowie D05";
    private DeviceWatcher _deviceWatcher;

    private void InitializeDeviceWatcher()
    {
        string aqsFilter = $"System.Devices.Aep.DeviceContainer.Category:\"{{65A0EBA6-5783-4741-9027-33EFA8D2742C}}\" AND System.Devices.Aep.Bluetooth.Le.IsConnectable:System.StructuredQueryType.Boolean#True AND System.ItemNameDisplay:~\"{BTDeviceFriendlyName}\"";
        string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

        _deviceWatcher = DeviceInformation.CreateWatcher(aqsFilter, requestedProperties, DeviceInformationKind.AssociationEndpoint);

        _deviceWatcher.Added += DeviceWatcher_Added;
        _deviceWatcher.Updated += DeviceWatcher_Updated;
        _deviceWatcher.Removed += DeviceWatcher_Removed;

        _deviceWatcher.Start();
    }

    private static string GetDeviceAqsFilter(string deviceName)
    {
        return "System.Devices.Aep.DeviceContainer.Category:\"{{65A0EBA6-5783-4741-9027-33EFA8D2742C}}\""
            + "AND System.Devices.Aep.Bluetooth.Le.IsConnectable:System.StructuredQueryType.Boolean#True"
            + $"AND System.ItemNameDisplay:~\"{deviceName}\"";
    }

    /*
        private async Task CheckBatteryLevelForAllDevicesAsync()
        {
            var devices = await DeviceInformation.FindAllAsync(deviceWatcher.GetAqsFilter());
            foreach (var device in devices)
            {
                await CheckBatteryLevelAsync(device.Id);
            }
        }

    */

    private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
    {
        CheckBatteryLevelAsync(args.Id);
    }

    private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        CheckBatteryLevelAsync(args.Id);
    }

    private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        // Handle device removal if needed
    }

    public async Task<List<DeviceInformation>> ListDevicesAsync()
    {
        var aqsFilter = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var devices = await DeviceInformation.FindAllAsync(aqsFilter);
        return devices.ToList();

    }

    public async Task<List<DeviceInformation>> ListLEDevicesAsync()
    {
        var aqsFilter = BluetoothLEDevice.GetDeviceSelector();
        var devices = await DeviceInformation.FindAllAsync(aqsFilter);
        return devices.ToList();

    }

    public async Task<string?> FindDeviceIdAsync(string deviceName)
    {
        // var aqsFilter = BluetoothDevice.GetDeviceSelectorFromDeviceName(deviceName);
        var aqsFilter = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var devices = await DeviceInformation.FindAllAsync(aqsFilter);
        if ( devices is null ) return null;

        foreach ( var device in devices )
        {
            if ( device?.Name == deviceName ) return device.Id;

        }
        return null;

        // var device = devices[0];
        // if (device is null) return null;

        // return device.Id;
    }


    public async Task<string?> FindLEDeviceIdAsync(string deviceName)
    {
        // var aqsFilter = BluetoothDevice.GetDeviceSelectorFromDeviceName(deviceName);
        var aqsFilter = BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected);
        var devices = await DeviceInformation.FindAllAsync(aqsFilter);
        if ( devices is null ) return null;

        foreach ( var device in devices )
        {
            if ( device?.Name == deviceName ) return device.Id;

        }
        return null;

        // var device = devices[0];
        // if (device is null) return null;

        // return device.Id;
    }

    public async Task<int> CheckBatteryLevelAsync(string deviceId)
    {
        try
        {
            BluetoothLEDevice device = null;
            BluetoothDevice bluetoothDevice = null;
            try
            {
                device = await BluetoothLEDevice.FromIdAsync(deviceId);
            }
            catch ( ArgumentException ex )
            {
                Console.WriteLine(ex);
                bluetoothDevice = await BluetoothDevice.FromIdAsync(deviceId);
            }

            if ( bluetoothDevice is not null)
            {
                // You must connect to Battery Service (BLE GATT service) to be able to read and notified about batt level information. No other way to do that. Of course, if that is BLE device. it is is Classic Bluetooth device than you have to connec to its RFCOMM control channel (for HandsFree) or use L2CAP (not available on Windows) to read information from Audio service channel 9for A2DP device)
            }



            if ( device != null )
            {
                var batteryServices = await device.GetGattServicesForUuidAsync(GattServiceUuids.Battery);
                if ( batteryServices != null )
                {
                    var batteryService = batteryServices.Services[0];
                    var batteryLevelCharacteristic = await batteryService.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel);
                    var readResult = batteryLevelCharacteristic.Characteristics[0];
                    if ( readResult != null )
                    {
                        var value = await readResult.ReadValueAsync(BluetoothCacheMode.Uncached);
                        if ( value != null && value.Status == GattCommunicationStatus.Success )
                        {
                            byte[] valueBytes = value.Value.ToArray();
                            if ( valueBytes.Length > 0 )
                            {
                                int batteryLevel = valueBytes[0];
                                return batteryLevel;
                            }
                        }
                    }
                }
            }

            return -1;
        }
        catch ( Exception ex )
        {
            return -1; // MessageBox.Show($"Error checking battery level: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    // Add logic to connect to Classic Bluetooth device's RFCOMM control channel
    public async Task ConnectToClassicBluetoothDeviceAsync(string deviceId)
    {
        try
        {
            // Create a BluetoothDevice object
            BluetoothDevice bluetoothDevice = await BluetoothDevice.FromIdAsync(deviceId);
            if ( bluetoothDevice != null )
            {
                // Get the RFCOMM services
                var rfcommServices = await bluetoothDevice.GetRfcommServicesAsync();
                if (rfcommServices.Services.Count == 0)
                {
                    Console.WriteLine("No RFCOMM services found on the device.");
                    return;
                }

                RfcommDeviceService rfcommDeviceService = null;
                foreach (var rfccommservice in rfcommServices.Services )
                {
                    rfcommDeviceService = rfccommservice;
                    //break;
                }

                try
                {
                    StreamSocket socket = new StreamSocket();
                    await socket.ConnectAsync(rfcommDeviceService.ConnectionHostName, rfcommDeviceService.ConnectionServiceName);
                    Console.WriteLine("Connected to service: " + rfcommDeviceService.ServiceId.Uuid);

                    CancellationTokenSource source = new CancellationTokenSource();
                    CancellationToken cancelToken = source.Token;
                    Task listenOnChannel = new TaskFactory().StartNew(async () =>
                    {
                        while ( true )
                        {
                            if ( cancelToken.IsCancellationRequested )
                            {
                                return;
                            }
                            await ReadWrite.Read(socket, source);
                        }
                    }, cancelToken);

                    await Task.Delay(5000);

                }
                catch ( Exception e )
                {
                    Console.WriteLine("Could not connect to service " + e.Message);
                    return;
                }
            }
        }
        catch ( Exception ex )
        {
            Console.WriteLine($"Error connecting to Classic Bluetooth device: {ex.Message}");
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

    static class ReadWrite
    {
        public static async Task Read(StreamSocket socket, CancellationTokenSource source)
        {
            IBuffer buffer = new Windows.Storage.Streams.Buffer(1024);
            uint bytesRead = 1024;

            IBuffer result = await socket.InputStream.ReadAsync(buffer, bytesRead, InputStreamOptions.Partial);
            await Write(socket, "OK");

            DataReader reader = DataReader.FromBuffer(result);
            var output = reader.ReadString(result.Length);

            if ( output.Length != 0 )
            {
                Console.WriteLine("Recieved :" + output.Replace("\r", " "));
                if ( output.Contains("IPHONEACCEV") )
                {
                    try
                    {
                        var batteryCmd = output.Substring(output.IndexOf("IPHONEACCEV"));
                        Console.WriteLine("Battery level :" + (Int32.Parse(batteryCmd.Substring(batteryCmd.LastIndexOf(",") + 1)) + 1) * 10);
                        source.Cancel();
                    }
                    catch ( Exception e )
                    {
                        Console.WriteLine("Could not retrieve " + e.Message);
                    }
                }
            }
        }

        public static async Task Write(StreamSocket socket, string str)
        {
            var bytesWrite = CryptographicBuffer.ConvertStringToBinary("\r\n" + str + "\r\n", BinaryStringEncoding.Utf8);
            await socket.OutputStream.WriteAsync(bytesWrite);
        }
    }
}
