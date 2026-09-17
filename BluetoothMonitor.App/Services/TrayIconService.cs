using System;
using System.IO;
using H.NotifyIcon.Core;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

namespace BluetoothMonitor.App.Services;

public sealed class TrayIconService : ITrayIconService
{
    private readonly ISettingsService _settings;
    private readonly IBatteryPollingService _polling;
    private TrayIconWithContextMenu? _icon;
    private PopupMenu? _menu;
    private byte? _lastLevel;
    private bool _lastConnected;
    private readonly ILogger<TrayIconService> _logger;

    public TrayIconService(
        ISettingsService settings,
        IBatteryPollingService polling,
        ILogger<TrayIconService> logger
    )
    {
        _settings = settings;
        _polling = polling;
        _logger = logger;
        _polling.BatteryUpdated += OnBatteryUpdated;
    }

    public void Initialize()
    {
        _menu = new PopupMenu();
        _menu.Items.Add(
            new PopupMenuItem(
                AppResources.Get("Tray.Show"),
                (_, _) => App.Current.MainWindow?.ShowWindow()
            )
        );
        _menu.Items.Add(
            new PopupMenuItem(
                AppResources.Get("Tray.Rescan"),
                (_, _) => _ = _polling.PollOnceAsync()
            )
        );
        _menu.Items.Add(
            new PopupMenuItem(
                AppResources.Get("Tray.Settings"),
                (_, _) => App.Current.MainWindow?.ShowWindow()
            )
        );
        _menu.Items.Add(new PopupMenuSeparator());
        _menu.Items.Add(
            new PopupMenuItem(
                AppResources.Get("Tray.Exit"),
                (_, _) => App.Current.MainWindow?.ExitApplication()
            )
        );

        _icon = new TrayIconWithContextMenu
        {
            ToolTip = $"{_settings.Current.DeviceName}",
            ContextMenu = _menu,
        };
        _icon.MessageWindow.MouseEventReceived += (_, args) =>
        {
            if (args.MouseEvent is MouseEvent.IconLeftDoubleClick or MouseEvent.IconLeftMouseUp)
            {
                App.Current.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                    App.Current.MainWindow?.ShowWindow()
                );
            }
        };

        ApplyIcon();
        _icon.Create();
        _logger.LogInformation("Tray icon initialized");
    }

    public void UpdateIcon(byte? level, bool connected)
    {
        _lastLevel = level;
        _lastConnected = connected;
        ApplyIcon();
    }

    private void ApplyIcon()
    {
        if (_icon is null)
            return;

        var fileName = !_lastConnected
            ? "tray-offline.ico"
            : _lastLevel switch
            {
                null => "tray-offline.ico",
                <= 5 => "tray-critical.ico",
                <= 20 => "tray-low.ico",
                _ => "tray-connected.ico",
            };
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        var fallback = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        var chosen = File.Exists(path) ? path : (File.Exists(fallback) ? fallback : null);

        if (chosen is not null)
        {
            try
            {
                var icon = new System.Drawing.Icon(chosen);
                _icon.UpdateIcon(icon.Handle);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tray icon update failed");
            }
        }

        var label = _lastLevel is byte b ? $" {b}%" : string.Empty;
        _icon.UpdateToolTip($"{_settings.Current.DeviceName}{label}");
    }

    private void OnBatteryUpdated(object? sender, BatteryUpdatedEventArgs e)
    {
        UpdateIcon(e.Level, e.Level is not null);
    }

    public void Dispose()
    {
        _polling.BatteryUpdated -= OnBatteryUpdated;
        _icon?.Dispose();
    }
}
