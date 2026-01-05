using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Storage.Streams;
using Windows.Security.Cryptography;
using Windows.Networking.Sockets;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace BluetoothBatteryReader
{
    enum DeviceKind { Unknown, BLE, Classic, Dual }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Available paired devices..");
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true));

            if (devices.Count == 0)
            {
                Console.WriteLine("No devices found");
                Exit();
            }

            for (int i = 0; i < devices.Count; i++)
            {   
                var device = devices[i];
                var kind = await GetDeviceKindAsync(device.Id);
                Console.WriteLine(String.Format("{0}. {1}\t{2}\t{3}", i, device.Name, device.Id, kind));
            }

            Console.WriteLine("Select a device to connect..");
            int selectedDevice = int.Parse(Console.ReadLine());

            BluetoothDevice blDevice = await BluetoothDevice.FromIdAsync(devices[selectedDevice].Id);

            if (blDevice != null)
            {
                // Check for GATT Battery Service first (BLE devices commonly expose battery level via GATT)
                try
                {
                    var btLeDevice = await BluetoothLEDevice.FromIdAsync(devices[selectedDevice].Id);
                    Console.WriteLine("Checking GATT services for Battery...");
                    var gattServicesResult = await btLeDevice.GetGattServicesAsync();
                    if (gattServicesResult.Status == GattCommunicationStatus.Success)
                    {
                        var batteryService = gattServicesResult.Services.FirstOrDefault(s => s.Uuid == GattServiceUuids.Battery);
                        if (batteryService != null)
                        {
                            Console.WriteLine("Battery GATT service found. Reading battery level...");
                            var charsResult = await batteryService.GetCharacteristicsAsync();
                            if (charsResult.Status == GattCommunicationStatus.Success)
                            {
                                var batteryChar = charsResult.Characteristics.FirstOrDefault(c => c.Uuid == GattCharacteristicUuids.BatteryLevel);
                                if (batteryChar != null)
                                {
                                    var readResult = await batteryChar.ReadValueAsync();
                                    if (readResult.Status == GattCommunicationStatus.Success)
                                    {
                                        var reader = DataReader.FromBuffer(readResult.Value);
                                        byte level = reader.ReadByte();
                                        Console.WriteLine($"Battery level (GATT): {level}%");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Could not read battery level: {readResult.Status}");
                                    }

                                    // Subscribe to notifications if supported
                                    var cccdResult = await batteryChar.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                    Console.WriteLine($"Enable notifications result: {cccdResult}");
                                    batteryChar.ValueChanged += (s, e) =>
                                    {
                                        try
                                        {
                                            var dr = DataReader.FromBuffer(e.CharacteristicValue);
                                            byte lvl = dr.ReadByte();
                                            Console.WriteLine($"Battery notification: {lvl}%");
                                        }
                                        catch { }
                                    };

                                    // If battery was found and read, we can continue but inform the user
                                    Console.WriteLine("GATT battery handled; will still list RFCOMM services below if present.");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("No Battery GATT service found.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Could not enumerate GATT services: {gattServicesResult.Status}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("GATT check failed: " + ex.Message);
                }

                Console.WriteLine("Discovering Rfcomm services..");
                RfcommDeviceServicesResult rfcommResult = await blDevice.GetRfcommServicesAsync();
                if (rfcommResult.Services.Count == 0)
                {
                    Console.WriteLine("No services found");
                    Exit();
                }
                for (int i = 0; i < rfcommResult.Services.Count; i++)
                {
                    var service = rfcommResult.Services[i];
                    var desc = GetRfcommServiceDescription(service.ServiceId.Uuid);
                    Console.WriteLine($"{i}. {service.ServiceId.Uuid} ({desc}): {service.ConnectionServiceName}: {service.DeviceAccessInformation.CurrentStatus}");
                }
                Console.WriteLine("Select a service to connect..");
                int selectedService = Int32.Parse(Console.ReadLine());

                try
                {
                    StreamSocket socket = new StreamSocket();
                    await socket.ConnectAsync(rfcommResult.Services[selectedService].ConnectionHostName, rfcommResult.Services[selectedService].ConnectionServiceName);
                    Console.WriteLine("Connected to service: " + rfcommResult.Services[selectedService].ServiceId.Uuid);

                    // Probe the device with several common commands that accessories might respond to
                    await ProbeDeviceAsync(socket);

                    CancellationTokenSource source = new CancellationTokenSource();
                    CancellationToken cancelToken = source.Token;
                    Task listenOnChannel = new TaskFactory().StartNew(async () =>
                    {
                        while (true)
                        {
                            if (cancelToken.IsCancellationRequested)
                            {
                                return;
                            }
                            await ReadWrite.Read(socket, source);
                        }
                    }, cancelToken);

                    Console.ReadKey();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not connect to service " + e.Message);
                    Exit();
                }
            }
            else
            {
                Exit();
            }
        }

        // Probe device by sending a set of common commands over RFCOMM/SPP
        static async Task ProbeDeviceAsync(StreamSocket socket)
        {
            var probes = new[]
            {
                "AT+IPHONEACCEV",
                "AT+IPHONEACCEV?",
                "AT+BAT?",
                "AT+GETBAT",
                "AT+GET_BATTERY",
                "GET BATTERY",
                "GET_BAT",
                "BATTERY",
                "STATUS",
                "AT+STATUS",
                "\r\n" // send a newline to trigger any prompt
            };

            foreach (var p in probes)
            {
                try
                {
                    await ReadWrite.Write(socket, p);
                    // small delay to allow device to respond
                    await Task.Delay(300);
                }
                catch { }
            }
        }

        // Determine whether a device is BLE (GATT), Classic (Rfcomm) or both.
        static async Task<DeviceKind> GetDeviceKindAsync(string deviceId)
        {
            bool isBle = false;
            bool isClassic = false;

            try
            {
                var le = await BluetoothLEDevice.FromIdAsync(deviceId);
                if (le != null)
                {
                    isBle = true;
                }
            }
            catch { /* ignore */ }

            try
            {
                var bd = await BluetoothDevice.FromIdAsync(deviceId);
                if (bd != null)
                {
                    try
                    {
                        var rf = await bd.GetRfcommServicesAsync();
                        if (rf?.Services != null && rf.Services.Count > 0)
                        {
                            isClassic = true;
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }

            if (isBle && isClassic) return DeviceKind.Dual;
            if (isBle) return DeviceKind.BLE;
            if (isClassic) return DeviceKind.Classic;
            return DeviceKind.Unknown;
        }

        static string GetRfcommServiceDescription(Guid uuid)
        {
            // Common RFCOMM/SDP service UUIDs
            var map = new System.Collections.Generic.Dictionary<Guid, string>(System.Collections.Generic.EqualityComparer<Guid>.Default)
            {
                { Guid.Parse("00001101-0000-1000-8000-00805f9b34fb"), "Serial Port (SPP)" },
                { Guid.Parse("00001108-0000-1000-8000-00805f9b34fb"), "Headset (HS)" },
                { Guid.Parse("0000111e-0000-1000-8000-00805f9b34fb"), "Handsfree (HFP)" },
                { Guid.Parse("0000110b-0000-1000-8000-00805f9b34fb"), "Audio Source (A2DP)" },
                { Guid.Parse("0000110e-0000-1000-8000-00805f9b34fb"), "Remote Control (AVRCP)" },
                { Guid.Parse("00001105-0000-1000-8000-00805f9b34fb"), "OBEX Object Push" },
                { Guid.Parse("00001106-0000-1000-8000-00805f9b34fb"), "OBEX File Transfer" }
            };

            if (map.TryGetValue(uuid, out var name)) return name;

            // If it's a 16-bit assigned number embedded in the UUID, try to extract it
            const string basePattern = "-0000-1000-8000-00805f9b34fb";
            var s = uuid.ToString().ToLower();
            if (s.EndsWith(basePattern))
            {
                var shortId = s.Substring(0, 8);
                return $"Assigned UUID (0x{shortId.Substring(4)})";
            }

            return "Unknown RFCOMM Service";
        }

        static void Exit()
        {
            Console.ReadKey();
            Environment.Exit(1);
        }
    }

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

                // First check for IPHONEACCEV as before
                if (output.Contains("IPHONEACCEV"))
                {
                    try
                    {
                        var batteryCmd = output.Substring(output.IndexOf("IPHONEACCEV"));
                        Console.WriteLine("Battery level :" + (Int32.Parse(batteryCmd.Substring(batteryCmd.LastIndexOf(",") + 1)) + 1) * 10);
                        source.Cancel();
                        return;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Could not retrieve " + e.Message);
                    }
                }

                // Try to detect common battery keywords and numeric values
                try
                {
                    // percent pattern e.g. "85%"
                    var m = Regex.Match(output, "(\\d{1,3})\\s*%");
                    if (m.Success)
                    {
                        if (int.TryParse(m.Groups[1].Value, out int val) && val >= 0 && val <= 100)
                        {
                            Console.WriteLine($"Battery level (parsed %): {val}%");
                            source.Cancel();
                            return;
                        }
                    }

                    // battery: 85 or Battery=85
                    m = Regex.Match(output, "battery[^0-9]{0,3}([0-9]{1,3})", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        if (int.TryParse(m.Groups[1].Value, out int val) && val >= 0 && val <= 100)
                        {
                            Console.WriteLine($"Battery level (parsed battery): {val}%");
                            source.Cancel();
                            return;
                        }
                    }

                    // generic number fallback (first number between 0 and 100)
                    m = Regex.Match(output, "\\b([0-9]{1,3})\\b");
                    if (m.Success)
                    {
                        if (int.TryParse(m.Groups[1].Value, out int val) && val >= 0 && val <= 100)
                        {
                            Console.WriteLine($"Battery level (parsed number): {val}%");
                            source.Cancel();
                            return;
                        }
                    }
                }
                catch { }
            }
        }

        public static async Task Write(StreamSocket socket, string str)
        {
            Console.WriteLine("Sending :" + str);
            var bytesWrite = CryptographicBuffer.ConvertStringToBinary("\r\n" + str + "\r\n", BinaryStringEncoding.Utf8);
            await socket.OutputStream.WriteAsync(bytesWrite);
        }
    }
}