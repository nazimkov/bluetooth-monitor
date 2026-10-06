using System;
using System.Threading.Tasks;
using Windows.System;

namespace BluetoothMonitor.App.Services;

public sealed class WindowsUriLauncher : IUriLauncher
{
    public async Task<bool> LaunchAsync(Uri uri) => await Launcher.LaunchUriAsync(uri);
}
