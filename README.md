# Bluetooth Monitor

Bluetooth battery monitor for Windows.

![Bluetooth Monitor main page](docs/main-page.png)

## Main features

Bluetooth Monitor is a Windows tray application that:

- Finds paired Bluetooth audio devices and shows their connection state.
- Reads battery levels for Bluetooth Low Energy and Classic Bluetooth devices.
- Shows one monitored device in the main window and keeps the selected device after navigation.
- Refreshes device data on demand and at a configurable interval.
- Shows low and critical battery warnings in the app and in Windows notifications.
- Lets you set the low-battery threshold, notification style, alert sound, and critical-alert option.
- Supports System, Light, and Dark themes.
- Can start at sign-in and keep running when the window closes.
- Runs in the system tray and provides Show, Rescan, Settings, and Exit actions.
- Provides a Pair new action that opens Windows Bluetooth settings.
- Stores settings locally. Bluetooth polling does not send data over the network.

The current V1 scope has these limits:

- Earbud left, right, and case levels are not separate. The app shows one level.
- Bluetooth codec information is not available from Windows.
- Do Not Disturb silencing is displayed but disabled because Windows does not expose the required state.
- Settings search, automatic updates, and in-app pairing are not implemented.

## Install the portable app

The portable build does not need .NET or the Windows App SDK. It runs on
64-bit Windows 10 version 1809 or later, and on Windows 11.

1. Open the project's **Releases** page on GitHub.
2. Download `BluetoothMonitor-win-x64.zip` from the latest release.
3. Extract the ZIP file to a folder, such as `C:\Apps\BluetoothMonitor`.
4. Run `BluetoothMonitor.exe` from the extracted folder.

Keep all files in the extracted folder. The executable needs the files beside
it to start correctly.

The app stores its settings in:

`%LOCALAPPDATA%\BluetoothMonitor\settings.json`

To remove the app, close it and delete the extracted folder. To also remove
your settings, delete the `BluetoothMonitor` folder under `%LOCALAPPDATA%`.

## Build from source

Install the .NET 8 SDK and the Windows 10 SDK version 22621 or later. Then run:

```powershell
dotnet build BluetoothMonitor.sln -c Debug -p:Platform=x64
```

The portable package is built by the GitHub Actions workflow. Start it from
**Actions > Build portable app > Run workflow**, or push a tag that starts with
`v`, such as `v1.0.0`.
