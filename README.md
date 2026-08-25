# BluetoothMonitor

Bluetooth battery monitor for Windows.

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
