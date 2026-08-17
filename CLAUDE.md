# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Restore and build the whole solution (x64 only — the WinUI app is not AnyCPU)
dotnet build BluetoothMonitor.sln -c Debug -p:Platform=x64

# Run the tray app (launches the WinUI 3 window and sits in the system tray)
dotnet run --project BluetoothMonitor.App -c Debug -r win-x64
```

There are no tests. The app targets `net8.0-windows10.0.22621.0` so it requires the Windows 10 22621 SDK and a Windows host. The WinUI project is packaged (MSIX) — first build will auto-generate a `BluetoothMonitor.App_TemporaryKey.pfx` signing cert.

## Architecture

Two projects, both `net8.0-windows10.0.22621.0`:

- **`BluetoothMonitor.Core`** — class library that owns all Bluetooth-battery reads. **Do not modify unless the change is purely additive; the App layer owns all UI/product logic.** Key types:
  - `IBluetoothDevices` contract with `ListDevicesAsync`, `FindDeviceIdAsync`, `CheckBatteryLevelAsync` returning `DeviceBatteryLevel(string, byte)`.
  - `BluetoothLEDevices` — WinRT `BluetoothLEDevice.FromIdAsync` + GATT Battery service/characteristic. Only path that works for LE devices; won't work for Classic.
  - `BluetoothClassicDevices` — P/Invoke into `setupapi.dll` (wrappers in `Utils/SetupAPI.cs`) to read the `DEVPKEY_DEVICE_BATTERY` property. Only path that works for Classic BR/EDR devices; the GATT battery characteristic is not available for them.
  - `BluetoothException` wraps all failure paths.

- **`BluetoothMonitor.App`** — WinUI 3 packaged app (MSIX, `WindowsPackageType=MSIX`, `Platforms=x64`). Built around the "Battcheck" Claude-Design mockup. Structure:
  - `Program.cs` wires `Application.Start` with a single-instance redirection check before any XAML loads. `App.xaml.cs` hosts the DI container, settings load, AppNotifications registration, main-window bootstrap, and tray-icon init.
  - `MainWindow` is a 920×640 Mica window with `ExtendsContentIntoTitleBar`, a `NavigationView` sidebar (Devices / Notifications / General / About), and close-to-tray behavior gated on `SettingsModel.KeepRunningInBackground`.
  - `Services/BluetoothFacade` is the composition root replacing the old WinForms `BluetoothService`: it owns one `BluetoothClassicDevices` + one `BluetoothLEDevices`, dispatches battery reads by probing `BluetoothLEDevice.FromIdAsync` (LE first, Classic fallback), and caches that result per device id so polling doesn't re-probe every tick.
  - `Services/DeviceCatalog` is the single source of truth for live device state. Devices page and tray flyout both bind to `DeviceCatalog.Devices`.
  - `Services/BatteryPollingService` owns a `DispatcherQueueTimer` keyed to `SettingsModel.RefreshIntervalSeconds`. It raises `BatteryUpdated` events; `DeviceCatalog` + `TrayIconService` + `DevicesViewModel` subscribe. Threshold-crossing logic fires low + critical toasts with hysteresis (low-notified flag resets when level climbs ≥ threshold+5).
  - `Services/JsonSettingsService` persists to `%LOCALAPPDATA%\Battcheck\settings.json` with a 500ms debounce.
  - `Services/TrayIconService` wraps `H.NotifyIcon.Core.TrayIconWithContextMenu`. Icon .ico swaps by level bucket (connected / low / critical / offline). MenuFlyout: Show, Rescan, Settings, Exit.
  - `Services/AppNotificationService` uses `AppNotificationBuilder` — MSIX package identity means action-button callbacks work without COM activator setup.
  - `Services/StartupTaskService` calls `Windows.ApplicationModel.StartupTask.GetAsync("BattcheckStartup")` — that StartupTask is declared in `Package.appxmanifest`. Relies on MSIX identity.
  - Custom controls (`BatteryRing`, `BatteryBar`, `HeroCard`, `DeviceRow`) implement the design's hero + list UI. `BatteryRing` draws a percent arc with `ArcSegment` (WinUI's `Ellipse` does not support stroke-dasharray).

### Design → WinUI mapping (quick reference)

- Mockup's CSS vars → `Styles/Colors.xaml` with Light/Dark `ThemeDictionaries`. Consumers bind `{ThemeResource BattcheckAccentBrush}` etc.
- Mockup's settings rows → `CommunityToolkit.WinUI.Controls.SettingsCard`.
- Mockup's banners → `InfoBar` with Severity mapping info/warn/danger → Informational/Warning/Error.
- Mockup's window chrome → `MicaBackdrop` + `ExtendsContentIntoTitleBar`; caption buttons are drawn by Windows.
- Mockup's tray badge → not rendered in-app; the OS tray renders our .ico set with numeric text in tooltip.

### Intentionally scoped out of V1 (design-vs-backend gaps)

- L/R/case sub-batteries for earbuds (Core returns a single byte — hero shows one ring).
- Codec display (Windows does not expose A2DP negotiated codec).
- DND / Focus-assist silencing (no public Windows API). The toggle row is rendered but disabled with an explanatory tooltip.
- "Find a setting" sidebar search (disabled `AutoSuggestBox` placeholder).
- "Check for updates" (shows an `InfoBar` "no update server configured").
- "Pair new" in-app pairing (deep-links to `ms-settings:bluetooth?&pair` instead).

### Gotchas

- **WinUI is x64 only.** The Core library builds for AnyCPU and maps `x64` → `AnyCPU` in the sln. The App maps `Any CPU` → `x64` (no build) so the sln's AnyCPU row is effectively "just build Core".
- Polling tick is created with `DispatcherQueue.GetForCurrentThread()` — it must start on the UI thread. That happens via `App.OnLaunched` calling `polling.StartAsync`.
- `JsonSettingsService.Update` raises `Changed` synchronously; multiple subscribers must not block. The polling service reuses the same event to re-arm its timer interval.
- `H.NotifyIcon.WinUI` tray icon is created programmatically (not in XAML) so it survives DI lifetime cleanly. Icon .ico files are loaded from the app's output `Assets\` folder.
- MSIX identity is load-bearing for the `StartupTask` API and for interactive-button toasts. Do not switch to unpackaged without rewriting `StartupTaskService` (HKCU Run key) and pruning `AppNotificationBuilder` buttons.
