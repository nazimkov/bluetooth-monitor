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
    private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcher =
        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private DeviceItemViewModel? _selectedDevice;

    [ObservableProperty]
    private bool _isBluetoothOff;

    [ObservableProperty]
    private bool _isLowBattery;

    [ObservableProperty]
    private bool _isCriticalBattery;

    public ObservableCollection<DeviceItemViewModel> Devices => _catalog.Devices;
    public bool HasNoDevices => Devices.Count == 0 && !IsBluetoothOff;

    public DevicesViewModel(
        ISettingsService settings,
        IDeviceCatalog catalog,
        IBatteryPollingService polling
    )
    {
        _settings = settings;
        _catalog = catalog;
        _polling = polling;

        _catalog.Refreshed += OnCatalogRefreshed;
        _catalog.Devices.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoDevices));
        _polling.BatteryUpdated += OnBatteryUpdated;
        _settings.Changed += OnSettingsChanged;

        // Run on the UI dispatcher (not a thread-pool thread) so the shared
        // catalog collection and bound properties (e.g. SelectedDevice) are
        // mutated on the UI thread. Otherwise, when this ViewModel is
        // recreated (it's Transient) on tab navigation, the restored
        // selection can silently fail to reach the UI.
        if (_dispatcher is not null)
        {
            _dispatcher.TryEnqueue(async () => await RefreshAndPollAsync());
        }
        else
        {
            _ = Task.Run(async () => await RefreshAndPollAsync());
        }
    }

    private async Task RefreshAndPollAsync()
    {
        await _catalog.RefreshAsync();
        await _polling.PollOnceAsync();
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
        UpdateBatteryBanners(SelectedDevice?.BatteryLevel);
    }

    private void ApplySelection()
    {
        var id = _settings.Current.SelectedDeviceId;
        foreach (var d in Devices)
            d.IsMonitored = false;
        var match = _catalog.FindById(id);
        if (match is not null)
        {
            match.IsMonitored = true;
        }
        SelectedDevice = match;
        UpdateBatteryBanners(SelectedDevice?.BatteryLevel);
    }

    private void UpdateBatteryBanners(byte? level)
    {
        var threshold = _settings.Current.LowBatteryMaximum;
        IsCriticalBattery =
            level.HasValue
            && level.Value <= _settings.Current.CriticalBatteryMaximum
            && _settings.Current.CriticalAlertUnder5;
        IsLowBattery =
            level.HasValue
            && level.Value <= threshold
            && level.Value > _settings.Current.CriticalBatteryMaximum;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsScanning = true;
        try
        {
            await _catalog.RefreshAsync();
            await _polling.PollOnceAsync();
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
        if (device is null)
            return;
        _settings.Update(s => s.SelectedDeviceId = device.Id);
    }

    [RelayCommand]
    private async Task OpenBluetoothSettings()
    {
        await Launcher.LaunchUriAsync(new Uri(AppResources.Get("Url.BluetoothSettings")));
    }

    [RelayCommand]
    private async Task PairNew()
    {
        await Launcher.LaunchUriAsync(new Uri(AppResources.Get("Url.PairBluetoothDevice")));
    }

    public void Dispose()
    {
        _catalog.Refreshed -= OnCatalogRefreshed;
        _polling.BatteryUpdated -= OnBatteryUpdated;
        _settings.Changed -= OnSettingsChanged;
    }
}
