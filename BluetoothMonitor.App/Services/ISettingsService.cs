using System;
using System.Threading.Tasks;
using BluetoothMonitor.App.Models;

namespace BluetoothMonitor.App.Services;

public interface ISettingsService
{
    SettingsModel Current { get; }
    event EventHandler? Changed;
    Task LoadAsync();
    Task SaveAsync();
    void Update(Action<SettingsModel> mutate);
}
