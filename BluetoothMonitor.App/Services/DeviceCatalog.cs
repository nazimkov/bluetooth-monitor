using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BluetoothMonitor.App.ViewModels;

namespace BluetoothMonitor.App.Services;

public sealed class DeviceCatalog : IDeviceCatalog
{
    private readonly IBluetoothFacade _facade;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public DeviceCatalog(IBluetoothFacade facade)
    {
        _facade = facade;
    }

    public ObservableCollection<DeviceItemViewModel> Devices { get; } = new();
    public bool IsRefreshing { get; private set; }
    public event EventHandler? Refreshed;

    public async Task RefreshAsync()
    {
        if (!await _refreshLock.WaitAsync(0))
            return;
        try
        {
            IsRefreshing = true;
            _facade.InvalidateCache();
            var list = await _facade.ListDevicesAsync();

            var byId = Devices.ToDictionary(d => d.Id);
            foreach (var info in list)
            {
                if (byId.TryGetValue(info.Id, out var existing))
                {
                    existing.Update(info);
                    byId.Remove(info.Id);
                }
                else
                {
                    Devices.Add(new DeviceItemViewModel(info));
                }
            }
            foreach (var stale in byId.Values)
            {
                Devices.Remove(stale);
            }
        }
        finally
        {
            IsRefreshing = false;
            _refreshLock.Release();
            Refreshed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ApplyBatteryUpdate(string deviceId, byte? level)
    {
        var match = FindById(deviceId);
        if (match is not null)
        {
            match.BatteryLevel = level;
        }
    }

    public DeviceItemViewModel? FindById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        return Devices.FirstOrDefault(d => d.Id == id);
    }
}
