using H.NotifyIcon.Core;
using Microsoft.Extensions.Logging;

namespace BluetoothMonitor.App.Services;

public sealed class TrayIconService : ITrayIconService
{
    private readonly ISettingsService _settings;
    private readonly IDeviceCatalog _catalog;
    private readonly IBatteryPollingService _polling;
    private TrayIconWithContextMenu? _icon;
    private PopupMenu? _menu;
    private byte? _lastLevel;
    private bool _lastConnected;
    private readonly ILogger<TrayIconService> _logger;

    public TrayIconService(
        ISettingsService settings,
        IDeviceCatalog catalog,
        IBatteryPollingService polling,
        ILogger<TrayIconService> logger
    )
    {
        _settings = settings;
        _catalog = catalog;
        _polling = polling;
        _logger = logger;
        _polling.BatteryUpdated += OnBatteryUpdated;
        _settings.Changed += OnSettingsChanged;
        _catalog.Refreshed += OnCatalogRefreshed;
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

        _icon = new TrayIconWithContextMenu { ToolTip = GetToolTipText(), ContextMenu = _menu };
        _icon.MessageWindow.MouseEventReceived += (_, args) =>
        {
            if (args.MouseEvent is MouseEvent.IconLeftDoubleClick or MouseEvent.IconLeftMouseUp)
            {
                _ = (
                    App.Current.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                        App.Current.MainWindow?.ShowWindow()
                    )
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

        var state = BatteryRangePolicy.Classify(_lastLevel, _lastConnected, _settings.Current);
        var fileName = state switch
        {
            BatteryIconState.Critical => GetLevelIconName("critical"),
            BatteryIconState.Low => GetLevelIconName("low"),
            BatteryIconState.Normal => GetLevelIconName("connected"),
            _ => "tray-offline.ico",
        };
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        var fallback = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        var chosen = File.Exists(path) ? path : (File.Exists(fallback) ? fallback : null);

        if (chosen is not null)
        {
            try
            {
                var icon = new System.Drawing.Icon(chosen);
                _ = _icon.UpdateIcon(icon.Handle);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tray icon update failed");
            }
        }

        _icon.UpdateToolTip(GetToolTipText());
    }

    private string GetLevelIconName(string state)
    {
        if (_lastLevel is not byte level)
            return $"tray-{state}.ico";

        // The ten icons show the nearest 10 percent level. The 10 percent icon is the minimum.
        var levelBucket = Math.Clamp(((int)level + 5) / 10 * 10, 10, 100);
        var levelIcon = $"tray-{state}-{levelBucket}.ico";
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", levelIcon);
        return File.Exists(path) ? levelIcon : $"tray-{state}.ico";
    }

    private string GetToolTipText()
    {
        var selectedName = _catalog.FindById(_settings.Current.SelectedDeviceId)?.Name;
        var deviceName = string.IsNullOrWhiteSpace(selectedName)
            ? _settings.Current.DeviceName
            : selectedName;
        var label = _lastLevel is byte level ? $" {level}%" : string.Empty;
        return $"{deviceName}{label}";
    }

    private void OnSettingsChanged(object? sender, EventArgs e) =>
        _icon?.UpdateToolTip(GetToolTipText());

    private void OnCatalogRefreshed(object? sender, EventArgs e) =>
        _icon?.UpdateToolTip(GetToolTipText());

    private void OnBatteryUpdated(object? sender, BatteryUpdatedEventArgs e) =>
        UpdateIcon(e.Level, e.IsConnected);

    public void Dispose()
    {
        _polling.BatteryUpdated -= OnBatteryUpdated;
        _settings.Changed -= OnSettingsChanged;
        _catalog.Refreshed -= OnCatalogRefreshed;
        _icon?.Dispose();
    }
}
