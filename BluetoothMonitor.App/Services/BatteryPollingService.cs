using System;
using System.Threading;
using System.Threading.Tasks;
using BluetoothMonitor.App.Models;
using Microsoft.UI.Dispatching;

namespace BluetoothMonitor.App.Services;

public sealed class BatteryPollingService : IBatteryPollingService, IDisposable
{
    private readonly ISettingsService _settings;
    private readonly IBluetoothFacade _facade;
    private readonly INotificationService _notifications;
    private DispatcherQueueTimer? _timer;
    private int _lastLevelAbove;
    private bool _lowNotified;
    private bool _criticalNotified;

    public BatteryPollingService(ISettingsService settings, IBluetoothFacade facade, INotificationService notifications)
    {
        _settings = settings;
        _facade = facade;
        _notifications = notifications;
        _settings.Changed += OnSettingsChanged;
    }

    public event EventHandler<BatteryUpdatedEventArgs>? BatteryUpdated;

    public Task StartAsync(CancellationToken ct)
    {
        var dq = DispatcherQueue.GetForCurrentThread()
                 ?? throw new InvalidOperationException("StartAsync must be called on the UI thread.");
        _timer = dq.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(Math.Max(5, _settings.Current.RefreshIntervalSeconds));
        _timer.Tick += (_, _) => _ = PollOnceAsync();
        _timer.IsRepeating = true;
        _timer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _timer?.Stop();
        return Task.CompletedTask;
    }

    public async Task PollOnceAsync()
    {
        var id = _settings.Current.SelectedDeviceId;
        if (string.IsNullOrWhiteSpace(id)) return;

        var level = await _facade.GetBatteryLevelAsync(id);
        BatteryUpdated?.Invoke(this, new BatteryUpdatedEventArgs { DeviceId = id, Level = level });

        if (level is null) return;
        HandleThresholds(id, level.Value);
    }

    private void HandleThresholds(string deviceId, byte level)
    {
        var settings = _settings.Current;
        var threshold = settings.LowBatteryThreshold;

        if (level >= threshold + 5)
        {
            _lowNotified = false;
            _criticalNotified = false;
            _lastLevelAbove = level;
            return;
        }

        if (level <= 5 && settings.CriticalAlertUnder5 && !_criticalNotified)
        {
            _notifications.ShowCriticalBattery(settings.DeviceName, level);
            _criticalNotified = true;
            _lowNotified = true;
            return;
        }

        if (level <= threshold && !_lowNotified && settings.NotificationStyle != NotificationStyle.Silent)
        {
            _notifications.ShowLowBattery(settings.DeviceName, level, settings.NotificationStyle);
            _lowNotified = true;
        }
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        if (_timer is null) return;
        var newInterval = TimeSpan.FromSeconds(Math.Max(5, _settings.Current.RefreshIntervalSeconds));
        if (_timer.Interval != newInterval)
        {
            _timer.Stop();
            _timer.Interval = newInterval;
            _timer.Start();
        }
    }

    public void Dispose()
    {
        _settings.Changed -= OnSettingsChanged;
        _timer?.Stop();
    }
}
