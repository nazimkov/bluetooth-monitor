using System.Collections.ObjectModel;
using BluetoothMonitor.App.ViewModels;

namespace BluetoothMonitor.App.Services;

public interface IDeviceCatalog
{
    ObservableCollection<DeviceItemViewModel> Devices { get; }
    bool IsRefreshing { get; }
    event EventHandler? Refreshed;
    Task RefreshAsync();
    void ApplyBatteryUpdate(string deviceId, byte? level, bool isConnected);
    DeviceItemViewModel? FindById(string? id);
}
