using BluetoothMonitor.App.Models;
using BluetoothMonitor.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BluetoothMonitor.App.ViewModels;

public partial class DeviceItemViewModel : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private DeviceKind _kind;
    [ObservableProperty] private string? _macAddress;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private byte? _batteryLevel;
    [ObservableProperty] private bool _isMonitored;
    [ObservableProperty] private string _lastSeen = "Just now";

    public DeviceItemViewModel() { }

    public DeviceItemViewModel(BluetoothDeviceInfo info)
    {
        Update(info);
    }

    public void Update(BluetoothDeviceInfo info)
    {
        Id = info.Id;
        Name = info.Name;
        Kind = info.Kind;
        MacAddress = info.MacAddress;
        IsConnected = info.IsConnected;
    }
}
