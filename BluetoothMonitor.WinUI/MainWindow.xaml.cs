using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BluetoothMonitor.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    ///
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static readonly string[] ProbeCommands =
       [
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
            "\r\n"
       ];

        private readonly List<RfcommDeviceService> _materializedServices = new();
        private DeviceEntry? _selectedDevice;
        private ServiceEntry? _selectedService;
        private bool _isBusy;
        private BluetoothDevice? _activeDevice;
        private StreamSocket? _socket;
        private CancellationTokenSource? _socketReaderCts;

        public ObservableCollection<DeviceEntry> Devices { get; } = new();
        public ObservableCollection<ServiceEntry> Services { get; } = new();
        public ObservableCollection<string> Logs { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        public MainWindow()
        {
            // Initialize the XAML UI first and set DataContext so bindings like {Binding Devices}
            // on the ListView find the properties on this instance.
            InitializeComponent();
            Closed += (_, _) => Cleanup();

            _ = RefreshDevicesAsync();
        }

        public DeviceEntry? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice == value)
                {
                    return;
                }

                _selectedDevice = value;
                OnPropertyChanged();
                _ = LoadServicesForSelectionAsync();
            }
        }

        public ServiceEntry? SelectedService
        {
            get => _selectedService;
            set
            {
                if (_selectedService == value)
                {
                    return;
                }

                _selectedService = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsConnectEnabled));
            }
        }
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value)
                {
                    return;
                }

                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanRefresh));
            }
        }

        public bool CanRefresh => !IsBusy;
        public bool IsConnectEnabled => SelectedService is { AccessStatus: DeviceAccessStatus.Allowed };
        public bool IsConnected => _socket is not null;

        private async void RefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDevicesAsync();
        }

        private async Task RefreshDevicesAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            AppendLog("Discovering paired devices...");

            SelectedDevice = null;
            SelectedService = null;
            Services.Clear();
            Devices.Clear();

            try
            {
                DeviceInformationCollection devices =
                    await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true));

                foreach (DeviceInformation device in devices)
                {
                    DeviceKind kind = await GetDeviceKindAsync(device.Id);
                    Devices.Add(new DeviceEntry(device.Name, device.Id, kind));
                }

                AppendLog($"Found {Devices.Count} paired devices.");
            }
            catch (Exception ex)
            {
                AppendLog($"Device discovery failed: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadServicesForSelectionAsync()
        {
            DisposeServices();

            if (SelectedDevice is null)
            {
                return;
            }

            AppendLog($"Discovering RFCOMM services for {SelectedDevice.DisplayName}...");

            try
            {
                IsBusy = true;
                _activeDevice?.Dispose();
                _activeDevice = await BluetoothDevice.FromIdAsync(SelectedDevice.Id);

                if (_activeDevice is null)
                {
                    AppendLog("Unable to open Bluetooth device handle for the selection.");
                    return;
                }

                RfcommDeviceServicesResult servicesResult = await _activeDevice.GetRfcommServicesAsync();
                if (servicesResult.Error != BluetoothError.Success)
                {
                    AppendLog($"Service discovery error: {servicesResult.Error}");
                }

                foreach (RfcommDeviceService service in servicesResult.Services)
                {
                    _materializedServices.Add(service);
                    Services.Add(new ServiceEntry(service));
                }

                AppendLog($"Loaded {Services.Count} RFCOMM services.");
            }
            catch (Exception ex)
            {
                AppendLog($"RFCOMM discovery failed: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            await ConnectSelectedServiceAsync();
        }

        private async Task ConnectSelectedServiceAsync()
        {
            if (SelectedService?.Service is null)
            {
                return;
            }

            AppendLog($"Connecting to {SelectedService.DisplayName}...");

            try
            {
                DisconnectInternal(log: false);

                var socket = new StreamSocket();
                await socket.ConnectAsync(SelectedService.Service.ConnectionHostName, SelectedService.Service.ConnectionServiceName);

                _socket = socket;
                _socketReaderCts = new CancellationTokenSource();
                OnPropertyChanged(nameof(IsConnected));

                AppendLog("Connected. Starting probe sequence...");
                _ = Task.Run(() => ReadLoopAsync(socket, _socketReaderCts.Token));
                await ProbeDeviceAsync(socket);
            }
            catch (Exception ex)
            {
                AppendLog($"Connection failed: {ex.Message}");
                DisconnectInternal(log: false);
            }
        }

        private async Task ProbeDeviceAsync(StreamSocket socket)
        {
            foreach (string command in ProbeCommands)
            {
                try
                {
                    string printable = command.Trim();
                    AppendLog($"> {(string.IsNullOrEmpty(printable) ? "(newline)" : printable)}");
                    var payload = CryptographicBuffer.ConvertStringToBinary("\r\n" + command + "\r\n", BinaryStringEncoding.Utf8);
                    await socket.OutputStream.WriteAsync(payload);
                    await Task.Delay(300);
                }
                catch (Exception ex)
                {
                    AppendLog($"Probe failed for '{command.Trim()}': {ex.Message}");
                }
            }
        }

        private async Task ReadLoopAsync(StreamSocket socket, CancellationToken token)
        {
            const uint bufferSize = 1024;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var buffer = new Windows.Storage.Streams.Buffer(bufferSize);
                    IBuffer result = await socket.InputStream.ReadAsync(buffer, bufferSize, InputStreamOptions.Partial);
                    if (result.Length == 0)
                    {
                        continue;
                    }

                    DataReader reader = DataReader.FromBuffer(result);
                    string payload = reader.ReadString(result.Length);

                    if (!string.IsNullOrWhiteSpace(payload))
                    {
                        string sanitized = payload.Replace("\r", " ").Replace("\n", " ").Trim();
                        AppendLog($"< {sanitized}");
                    }
                }
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                AppendLog($"Read loop stopped: {ex.Message}");
            }
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            DisconnectInternal();
        }

        private void DisconnectInternal(bool log = true)
        {
            _socketReaderCts?.Cancel();
            _socketReaderCts = null;

            if (_socket is not null)
            {
                try
                {
                    _socket.Dispose();
                }
                catch
                {
                    // ignore disposal issues
                }
            }

            _socket = null;
            OnPropertyChanged(nameof(IsConnected));

            if (log)
            {
                AppendLog("Disconnected.");
            }
        }

        private async Task<DeviceKind> GetDeviceKindAsync(string deviceId)
        {
            bool isBle = false;
            bool isClassic = false;

            try
            {
                using BluetoothLEDevice? le = await BluetoothLEDevice.FromIdAsync(deviceId);
                if (le is not null)
                {
                    isBle = true;
                }
            }
            catch
            {
                // ignore detection issues
            }

            try
            {
                using BluetoothDevice? classic = await BluetoothDevice.FromIdAsync(deviceId);
                if (classic is not null)
                {
                    isClassic = true;
                }
            }
            catch
            {
                // ignore detection issues
            }

            return (isBle, isClassic) switch
            {
                (true, true) => DeviceKind.Dual,
                (true, false) => DeviceKind.Ble,
                (false, true) => DeviceKind.Classic,
                _ => DeviceKind.Unknown
            };
        }

        private void DisposeServices()
        {
            foreach (RfcommDeviceService service in _materializedServices)
            {
                service.Dispose();
            }

            _materializedServices.Clear();
            Services.Clear();
            SelectedService = null;
        }

        private void AppendLog(string message)
        {
            string entry = $"{DateTime.Now:HH:mm:ss}  {message}";
            Logs.Add(entry);

            if (Logs.Count > 200)
            {
                Logs.RemoveAt(0);
            }
        }

        private void Cleanup()
        {
            DisconnectInternal(log: false);

            foreach (RfcommDeviceService service in _materializedServices)
            {
                service.Dispose();
            }

            _materializedServices.Clear();
            _activeDevice?.Dispose();
            _activeDevice = null;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public enum DeviceKind
    {
        Unknown,
        Ble,
        Classic,
        Dual
    }

    public sealed class DeviceEntry
    {
        public DeviceEntry(string name, string id, DeviceKind kind)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "(unnamed device)" : name;
            Id = id;
            Kind = kind;
        }

        public string Name { get; }
        public string Id { get; }
        public DeviceKind Kind { get; }
        public string DisplayName => Name;
        public string KindDisplay => Kind switch
        {
            DeviceKind.Ble => "BLE",
            DeviceKind.Classic => "Classic",
            DeviceKind.Dual => "Dual mode",
            _ => "Unknown"
        };
    }

    public sealed class ServiceEntry
    {
        private readonly string _displayName;
        private readonly string _detail;

        public ServiceEntry(RfcommDeviceService service)
        {
            Service = service;
            _displayName = $"{service.ServiceId.Uuid} ({BluetoothDescriptions.GetRfcommServiceDescription(service.ServiceId.Uuid)})";
            string host = service.ConnectionHostName?.DisplayName ?? "host unavailable";
            string name = string.IsNullOrWhiteSpace(service.ConnectionServiceName) ? "unknown channel" : service.ConnectionServiceName;
            _detail = $"{name} / {host}";
        }

        public RfcommDeviceService Service { get; }
        public DeviceAccessStatus AccessStatus => Service.DeviceAccessInformation.CurrentStatus;
        public string DisplayName => _displayName;
        public string Detail => _detail;
        public string AccessStatusText => AccessStatus.ToString();
    }

    internal static class BluetoothDescriptions
    {
        private static readonly Dictionary<Guid, string> ServiceMap = new()
        {
            { Guid.Parse("00001101-0000-1000-8000-00805f9b34fb"), "Serial Port (SPP)" },
            { Guid.Parse("00001108-0000-1000-8000-00805f9b34fb"), "Headset (HS)" },
            { Guid.Parse("0000111e-0000-1000-8000-00805f9b34fb"), "Handsfree (HFP)" },
            { Guid.Parse("0000110b-0000-1000-8000-00805f9b34fb"), "Audio Source (A2DP)" },
            { Guid.Parse("0000110e-0000-1000-8000-00805f9b34fb"), "Remote Control (AVRCP)" },
            { Guid.Parse("00001105-0000-1000-8000-00805f9b34fb"), "OBEX Object Push" },
            { Guid.Parse("00001106-0000-1000-8000-00805f9b34fb"), "OBEX File Transfer" }
        };

        public static string GetRfcommServiceDescription(Guid uuid)
        {
            if (ServiceMap.TryGetValue(uuid, out string? description))
            {
                return description;
            }

            const string suffix = "-0000-1000-8000-00805f9b34fb";
            string text = uuid.ToString().ToLowerInvariant();
            if (text.EndsWith(suffix, StringComparison.Ordinal))
            {
                string shortId = text[..8];
                return $"Assigned UUID (0x{shortId[4..]})";
            }

            return "Custom RFCOMM service";
        }
    }
}
