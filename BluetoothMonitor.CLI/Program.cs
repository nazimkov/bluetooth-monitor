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

namespace BluetoothMonitor.CLI
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


                Console.WriteLine("Reading charge from Setup API...");

                short charge = GetDeviceCharge(blDevice.DeviceId);

                Console.WriteLine($"Charge level (Setup API): {(charge >= 0 ? charge + "%" : "Unknown")}");

                if (charge >= 0)
                {
                    return;
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
                    var selectedRfcomm = rfcommResult.Services[selectedService];
                    StreamSocket socket = new StreamSocket();
                    await socket.ConnectAsync(selectedRfcomm.ConnectionHostName, selectedRfcomm.ConnectionServiceName);
                    Console.WriteLine("Connected to service: " + selectedRfcomm.ServiceId.Uuid);

                    // Probe the device with several common commands that accessories might respond to
                    //await ProbeDeviceAsync(socket);

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

        static short GetDeviceCharge(string id)
        {
            var btClassic = new BluetoothClassicDevices();
            if (btClassic.TryGetBatteryLevel(id, out var device))
            {
                return device.Charge;
            }
            return -1;
        }

        // Probe device by sending a set of common commands over RFCOMM/SPP
        static async Task ProbeDeviceAsync(StreamSocket socket)
        {
            var probes = new[]
            {
                "AT+IPHONEACCEV",
                //"AT+IPHONEACCEV?",
                //"AT+BAT?",
                //"AT+GETBAT",
                //"AT+GET_BATTERY",
                //"GET BATTERY",
                //"GET_BAT",
                //"BATTERY",
                //"STATUS",
                //"AT+STATUS",
                //"\r\n" // send a newline to trigger any prompt
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
            var map = new Dictionary<Guid, string>(EqualityComparer<Guid>.Default)
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
            // Keep reading packets until cancellation or until we parse a battery level
            while (!source.IsCancellationRequested)
            {
                IBuffer buffer = new Windows.Storage.Streams.Buffer(1024);
                uint bytesRead = 1024;

                IBuffer result;
                try
                {
                    result = await socket.InputStream.ReadAsync(buffer, bytesRead, InputStreamOptions.Partial);
                }
                catch (Exception)
                {
                    // Socket read failed or closed
                    return;
                }

                if (result == null || result.Length == 0)
                {
                    // No more data
                    return;
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
                        Console.WriteLine("Battery level :" + (int.Parse(batteryCmd.Substring(batteryCmd.LastIndexOf(",") + 1)) + 1) * 10);
                        source.Cancel();
                        return;
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
                                return;
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
                                return;
                            }
                            else if (p.Length >= 2 && int.TryParse(p[1], out int v2))
                            {
                                int overall = (v2 + 1) * 10;
                                Console.WriteLine($"Battery level (XEVENT): {overall}%");
                                source.Cancel();
                                return;
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
                }
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