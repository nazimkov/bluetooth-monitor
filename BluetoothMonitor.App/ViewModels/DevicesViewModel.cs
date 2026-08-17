using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BluetoothMonitor.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Windows.System;

namespace BluetoothMonitor.App.ViewModels;

public partial class DevicesViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settings;
    private readonly IDeviceCatalog _catalog;
    private readonly IBatteryPollingService _polling;

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private DeviceItemViewModel? _selectedDevice;
    [ObservableProperty] private bool _isBluetoothOff;
    [ObservableProperty] private bool _isLowBattery;
    [ObservableProperty] private bool _isCriticalBattery;

    public ObservableCollection<DeviceItemViewModel> Devices => _catalog.Devices;
    public bool HasNoDevices => Devices.Count == 0 && !IsBluetoothOff;

    public DevicesViewModel(ISettingsService settings, IDeviceCatalog catalog, IBatteryPollingService polling)
    {
        _settings = settings;
        _catalog = catalog;
        _polling = polling;

        _catalog.Refreshed += OnCatalogRefreshed;
        _catalog.Devices.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoDevices));
        _polling.BatteryUpdated += OnBatteryUpdated;
        _settings.Changed += OnSettingsChanged;

        _ = Task.Run(async () =>
        {
            await _catalog.RefreshAsync();
        });
    }

    private void OnCatalogRefreshed(object? sender, EventArgs e)
    {
        ApplySelection();
    }

    private void OnBatteryUpdated(object? sender, BatteryUpdatedEventArgs e)
    {
        _catalog.ApplyBatteryUpdate(e.DeviceId, e.Level);
        UpdateBatteryBanners(e.Level);
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        ApplySelection();
    }

    private void ApplySelection()
    {
        var id = _settings.Current.SelectedDeviceId;
        foreach (var d in Devices) d.IsMonitored = false;
        var match = _catalog.FindById(id);
        if (match is not null)
        {
            match.IsMonitored = true;
        }
        SelectedDevice = match;
    }

    private void UpdateBatteryBanners(byte? level)
    {
        var threshold = _settings.Current.LowBatteryThreshold;
        IsCriticalBattery = level.HasValue && level.Value <= 5 && _settings.Current.CriticalAlertUnder5;
        IsLowBattery = level.HasValue && level.Value <= threshold && level.Value > 5;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsScanning = true;
        try
        {
            await _catalog.RefreshAsync();
            await Task.Delay(TimeSpan.FromMilliseconds(2200));
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task PollNowAsync() => await _polling.PollOnceAsync();

    [RelayCommand]
    private void SelectDevice(DeviceItemViewModel? device)
    {
        if (device is null) return;
        _settings.Update(s => s.SelectedDeviceId = device.Id);
    }

    [RelayCommand]
    private async Task OpenBluetoothSettings()
    {
        await Launcher.LaunchUriAsync(new Uri("ms-settings:bluetooth"));
    }

    [RelayCommand]
    private async Task PairNew()
    {
        await Launcher.LaunchUriAsync(new Uri("ms-settings:bluetooth?&pair"));
    }

    public void Dispose()
    {
        _catalog.Refreshed -= OnCatalogRefreshed;
        _polling.BatteryUpdated -= OnBatteryUpdated;
        _settings.Changed -= OnSettingsChanged;
    }
}
