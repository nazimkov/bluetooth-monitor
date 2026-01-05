using System.Net.Sockets;
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
// https://github.com/inthehand/32feet/issues/75

public interface IBluetoothService
{
    Task<List<DeviceInformation>> ListDevicesAsync();
    Task<string?> FindDeviceIdAsync(string deviceName);
    Task<int> CheckBatteryLevelAsync(string deviceId);
}

internal sealed class BluetoothClassicDeviceService : IBluetoothService
{
    public async Task<List<DeviceInformation>> ListDevicesAsync()
    {
        var aqsFilter = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var devices = await DeviceInformation.FindAllAsync(aqsFilter);
        return devices.ToList();

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

    public async Task<int> CheckBatteryLevelAsync(string deviceId)
    {
        var device = await BluetoothDevice.FromIdAsync(deviceId);
        var rfcommServices = await device.GetRfcommServicesAsync();
        foreach (var service in rfcommServices.Services)
        {
            var (type, serviceDescription) = BluetoothDescriptions.GetRfcommServiceDescription(service.ServiceId.Uuid);
            if (type == RfcommServiceType.Handsfree || type == RfcommServiceType.Headset)
            {
                using StreamSocket socket = new ();
                CancellationTokenSource source = new ();
                CancellationToken cancelToken = source.Token;
                int? level = null;
                Task listenOnChannel = new TaskFactory().StartNew(async () =>
                {
                    while (true)
                    {
                        if (cancelToken.IsCancellationRequested)
                        {
                            break;
                        }
                        level = await ReadWrite.Read(socket, source);
                        if (level.HasValue)
                        {
                            return;
                        }
                    }
                }, cancelToken);

                if (level.HasValue)
                {
                    return level.Value;
                }
            }
        }
        throw new Exception($"Battery service not found for device with id: {deviceId}.");
    }


    internal static class ReadWrite
    {
        public static async Task<int?> Read(StreamSocket socket, CancellationTokenSource source)
        {
            // Keep reading packets until cancellation or until we parse a battery level
            while (!source.IsCancellationRequested)
            {
                var buffer = new Windows.Storage.Streams.Buffer(1024);
                const uint bytesRead = 1024;

                IBuffer result;
                try
                {
                    result = await socket.InputStream.ReadAsync(buffer, bytesRead, InputStreamOptions.Partial);
                }
                catch (Exception)
                {
                    // Socket read failed or closed
                    return null;
                }

                if (result == null || result.Length == 0)
                {
                    // No more data
                    return null;
                }

                DataReader reader = DataReader.FromBuffer(result);
                var output = reader.ReadString(result.Length);

                if (string.IsNullOrWhiteSpace(output))
                {
                    continue;
                }

                Console.WriteLine("Recieved :" + output.Replace("\r", " "));

                // Normalize for case-insensitive checks but keep original for parsing values
                var line = output.Trim();
                var up = line.ToUpperInvariant();

                try
                {
                    // Handle known RFCOMM/AT-like exchanges
                    if (up.Contains("BRSF"))
                    {
                        await Write(socket, "+BRSF: 1024");
                        await Write(socket, "OK");
                    }
                    else if (up.Contains("CIND="))
                    {
                        await Write(socket, "+CIND:(\"service\",(0-1)),(\"call\",(0-1)),(\"callsetup\",(0-3)),(\"callheld\",(0-2)),(\"battchg\",(0-5))");
                        await Write(socket, "OK");
                    }
                    else if (up.Contains("CIND?"))
                    {
                        await Write(socket, "+CIND: 0,0,0,0,3");
                        await Write(socket, "OK");
                    }
                    else if (up.Contains("BIND=?"))
                    {
                        await Write(socket, "+BIND: (2)");
                        await Write(socket, "OK");
                    }
                    else if (up.Contains("BIND?"))
                    {
                        await Write(socket, "+BIND: 2,1");
                        await Write(socket, "OK");
                    }
                    else if (up.Contains("XAPL="))
                    {
                        await Write(socket, "+XAPL=iPhone,7");
                        await Write(socket, "OK");
                    }
                    else if (up.Contains("IPHONEACCEV"))
                    {
                        // parse comma separated parameters after IPHONEACCEV
                        var batteryCmd = up.Substring(up.IndexOf("IPHONEACCEV"));
                        var batteryLevel = (int.Parse(batteryCmd.Substring(batteryCmd.LastIndexOf(",") + 1)) + 1) * 10;
                        Console.WriteLine("Battery level :" + batteryLevel);
                        source.Cancel();
                        return batteryLevel;
                    }
                    else if (up.Contains("BIEV="))
                    {
                        var eq = line.Split(new[] { '=' }, 2);
                        if (eq.Length > 1)
                        {
                            var p = eq[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                            if (p.Length >= 2 && p[0] == "2" && int.TryParse(p[1], out int v))
                            {
                                Console.WriteLine($"Battery level (BIEV): {v}%");
                                source.Cancel();
                                return v;
                            }
                        }
                    }
                    else if (up.Contains("XEVENT=BATTERY"))
                    {
                        var eq = line.Split(new[] { '=' }, 2);
                        if (eq.Length > 1)
                        {
                            var p = eq[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                            if (p.Length >= 3 && int.TryParse(p[1], out int a) && int.TryParse(p[2], out int b) && b != 0)
                            {
                                int percent = (int)((double)a / b * 100.0);
                                Console.WriteLine($"Battery level (XEVENT ratio): {percent}%");
                                source.Cancel();
                                return percent;
                            }
                            else if (p.Length >= 2 && int.TryParse(p[1], out int v2))
                            {
                                int overall = (v2 + 1) * 10;
                                Console.WriteLine($"Battery level (XEVENT): {overall}%");
                                source.Cancel();
                                return overall;
                            }
                        }
                    }
                    else
                    {
                        // Default acknowledgement so many accessories expect an OK/CRLF reply
                        await Write(socket, "OK");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not handle response: " + ex.Message);
                    return null;
                }
            }
            return null;
        }

        public static async Task Write(StreamSocket socket, string str)
        {
            Console.WriteLine("Sending :" + str);
            var bytesWrite = CryptographicBuffer.ConvertStringToBinary("\r\n" + str + "\r\n", BinaryStringEncoding.Utf8);
            await socket.OutputStream.WriteAsync(bytesWrite);
        }
    }

    internal enum RfcommServiceType : byte
    {
        SerialPort = 1,
        Headset,
        Handsfree,
        AudioSource,
        RemoteControl,
        ObexObjectPush,
        ObexFileTransfer,
        Custom
    }
    internal static class BluetoothDescriptions
    {
        private static readonly Dictionary<Guid, ValueTuple<RfcommServiceType, string>> ServiceMap = new()
        {
            { Guid.Parse("00001101-0000-1000-8000-00805f9b34fb"), (RfcommServiceType.SerialPort, "") },
            { Guid.Parse("00001108-0000-1000-8000-00805f9b34fb"), (RfcommServiceType.Headset, "") },
            { Guid.Parse("0000111e-0000-1000-8000-00805f9b34fb"), (RfcommServiceType.Handsfree, "") },
            { Guid.Parse("0000110b-0000-1000-8000-00805f9b34fb"), (RfcommServiceType.AudioSource, "") },
            { Guid.Parse("0000110e-0000-1000-8000-00805f9b34fb"), (RfcommServiceType.RemoteControl, "") },
            { Guid.Parse("00001105-0000-1000-8000-00805f9b34fb"), (RfcommServiceType.ObexObjectPush, "") },
            { Guid.Parse("00001106-0000-1000-8000-00805f9b34fb"), (RfcommServiceType.ObexFileTransfer, "") }
        };

        public static ValueTuple<RfcommServiceType, string> GetRfcommServiceDescription(Guid uuid)
        {
            if (ServiceMap.TryGetValue(uuid, out var typeDesc))
            {
                return typeDesc;
            }

            const string suffix = "-0000-1000-8000-00805f9b34fb";
            string text = uuid.ToString().ToLowerInvariant();
            if (text.EndsWith(suffix, StringComparison.Ordinal))
            {
                string shortId = text[..8];
                return (RfcommServiceType.Custom, $"Assigned UUID (0x{shortId[4..]})");
            }

            return (RfcommServiceType.Custom, "Custom RFCOMM service");
        }
    }

}

internal sealed class BluetoothLEDeviceService : IBluetoothService
{
    public async Task<List<DeviceInformation>> ListDevicesAsync()
    {
        var aqsFilter = BluetoothLEDevice.GetDeviceSelector();
        var devices = await DeviceInformation.FindAllAsync(aqsFilter);
        return devices.ToList();

    }

    public async Task<string?> FindDeviceIdAsync(string deviceName)
    {
        var aqsFilter = BluetoothLEDevice.GetDeviceSelectorFromDeviceName(deviceName);
        var devices = await DeviceInformation.FindAllAsync(aqsFilter);
        if (devices is null) return null;

        foreach (var device in devices)
        {
            if (device?.Name == deviceName) return device.Id;

        }
        return null;
    }

    public async Task<int> CheckBatteryLevelAsync(string deviceId)
    {
        try
        {
            var device = await BluetoothLEDevice.FromIdAsync(deviceId);

            var batteryServices = await device.GetGattServicesForUuidAsync(GattServiceUuids.Battery);
            if (batteryServices != null)
            {
                var batteryService = batteryServices.Services[0];
                var batteryLevelCharacteristic = await batteryService.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel);
                var readResult = batteryLevelCharacteristic.Characteristics[0];
                if (readResult != null)
                {
                    var value = await readResult.ReadValueAsync(BluetoothCacheMode.Uncached);
                    if (value != null && value.Status == GattCommunicationStatus.Success)
                    {
                        byte[] valueBytes = value.Value.ToArray();
                        if (valueBytes.Length > 0)
                        {
                            int batteryLevel = valueBytes[0];
                            return batteryLevel;
                        }
                    }

                }
            }
            throw new Exception($"Failed to read battery level from the device with id: {deviceId}."); // TODO : Custom exception
        }
        catch (ArgumentException ex)
        {
            throw new Exception($"The device ID: {deviceId} is invalid or the device is not a Bluetooth LE device.", ex);
        }
    }
}

public sealed class BluetoothService
{

    public BluetoothService() => InitializeDeviceWatcher();

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

            if (output.Length != 0)
            {
                Console.WriteLine("Recieved :" + output.Replace("\r", " "));
                if (output.Contains("IPHONEACCEV"))
                {
                    try
                    {
                        var batteryCmd = output.Substring(output.IndexOf("IPHONEACCEV"));
                        Console.WriteLine("Battery level :" + (Int32.Parse(batteryCmd.Substring(batteryCmd.LastIndexOf(",") + 1)) + 1) * 10);
                        source.Cancel();
                    }
                    catch (Exception e)
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

