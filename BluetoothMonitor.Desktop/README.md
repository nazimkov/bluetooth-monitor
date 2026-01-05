# BluetoothMonitor.Desktop

Packaged WinUI 3 front-end that wraps the existing Bluetooth monitoring logic so RFCOMM connections can be established with the necessary restricted capabilities declared in the MSIX manifest.

## Building & running (debug)

1. Open `BluetoothMonitor.sln` in Visual Studio 2022 17.10+ with the Windows App SDK workload installed.
2. Right-click **BluetoothMonitor.Desktop** → **Set as Startup Project**.
3. Press F5 to deploy and run. Visual Studio automatically registers the MSIX so the manifest capabilities (`bluetooth`, `bluetooth.rfcomm`, `bluetooth.genericAttributeProfile`) apply.

## Packaging for sideloading

1. In Visual Studio: **Project** → **Publish** → **Create App Packages...** → **Sideloading**.
2. Accept the default certificate (or supply your own) and finish the wizard.
3. Install the generated `.msixbundle` on your machine (double-click → Install). You must keep developer mode enabled or sign the package with a trusted certificate.

After installing, the packaged desktop app can enumerate paired Bluetooth devices, display their RFCOMM services, and open sockets that were previously blocked with `DeniedBySystem` when running from an unpackaged CLI.
