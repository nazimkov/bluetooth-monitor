# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Restore and build the whole solution
dotnet build BluetoothMonitor.sln

# Run the tray app (WinForms)
dotnet run --project BluetoothMonitor/BluetoothMonitor.csproj

# Release build, x64 (matches solution platforms)
dotnet build BluetoothMonitor.sln -c Release -p:Platform=x64
```

There is no test project in the solution.

Target framework is `net8.0-windows10.0.22621.0` — builds require the Windows 10 22621 SDK and only run on Windows. The `BluetoothMonitor.Core` project pulls in WinRT APIs (`Windows.Devices.Bluetooth`, GATT, `DeviceInformation`) via the Windows target framework, not a NuGet package.

## Architecture

Two projects:

- **`BluetoothMonitor.Core`** — reusable library. Defines `IBluetoothDevices` (list / find by name / read battery) with two implementations under `Devices/`:
  - `BluetoothLEDevices` uses WinRT `BluetoothLEDevice` + the GATT Battery service/characteristic (`GattServiceUuids.Battery`, `GattCharacteristicUuids.BatteryLevel`) to read battery over BLE.
  - `BluetoothClassicDevices` uses P/Invoke into `setupapi.dll` (wrappers in `Utils/SetupAPI.cs`) to enumerate devices and read the `DEVPKEY_DEVICE_BATTERY` property. This is the only reliable way to read battery for Classic BR/EDR devices on Windows — GATT is not available for them.
  - Failures inside an implementation are wrapped in `BluetoothException`; the success shape is the `DeviceBatteryLevel(string DeviceId, byte BatteryLevel)` record. Note the Core project also contains an `internal` `BluetoothService` skeleton that is currently unused.

- **`BluetoothMonitor`** — WinForms tray app (`OutputType=WinExe`, `UseWindowsForms=true`). `Program.cs` → `Form1` (empty partial) + `Form1.Designer.cs` (all real UI/handler code). The form has no visible window; its only surface is a `NotifyIcon` in the system tray whose double-click handler queries battery and shows a `MessageBox`. `BluetoothService` in this project is the composition root: it owns one `BluetoothClassicDevices` + one `BluetoothLEDevices`, merges `ListDevicesAsync` results by `Id`, prefers the LE implementation for `FindDeviceIdAsync`, and dispatches `GetDeviceBatteryLevel` by probing `BluetoothLEDevice.FromIdAsync(deviceId)` — if it returns a device, use LE; otherwise fall back to Classic.

### Gotchas when modifying the app

- The target device is hardcoded as `DeviceName = "Baseus Bowie D05"` in `Form1.Designer.cs` and again (unused) as `BTDeviceFriendlyName` in `BluetoothService.cs`. Changing the monitored device means editing `Form1.Designer.cs`.
- `InitializeTimer` sets up a 5-minute poll, but `Timer_Tick` currently does nothing — the commented-out `CheckBatteryLevelForAllDevicesAsync` / `UpdateBatteryLevel` block in `BluetoothService.cs` is the intended periodic-update / low-battery-balloon path and is the obvious place to wire real behavior.
- `InitializeDeviceWatcher` in `BluetoothService.cs` is entirely commented out; `DeviceWatcher_Added/Updated/Removed` handlers exist but are never subscribed. If you re-enable the watcher, also wire up the `_deviceWatcher` field.
- The notify icon loads from a relative path (`assets\cake_slice_dessert_food_icon.ico`). `BluetoothMonitor.csproj` copies `assets\*.*` to the output dir with `CopyToOutputDirectory=Always` — keep this if you add new assets, otherwise the app will throw at startup.
- Platform list in the `.sln` is `AnyCPU;x64`. The Core project is AnyCPU-only (x64 maps to AnyCPU in solution config), so only the app project has a real x64 build.
- `BluetoothMonitor.sln` previously contained `BluetoothMonitor.CLI`, `BluetoothMonitor.Desktop` (WPF), and `BluetoothMonitor.WinUI` projects. Their files are deleted on disk (see `git status`) but you may still see references in older commits — the current shipping surface is only the WinForms tray app.

## Planned: migrate UI to WinUI 3

The current WinForms front end (`BluetoothMonitor` project) is being replaced with a WinUI 3 app. New UI work should target WinUI 3, not WinForms. `BluetoothMonitor.Core` stays as-is — only the shell/host project changes. When building the replacement, keep the same tray-icon UX and reuse `IBluetoothDevices` / `BluetoothService` composition rather than rewriting the Bluetooth logic.
