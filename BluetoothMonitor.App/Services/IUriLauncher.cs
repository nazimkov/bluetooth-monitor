using System;
using System.Threading.Tasks;

namespace BluetoothMonitor.App.Services;

public interface IUriLauncher
{
    Task<bool> LaunchAsync(Uri uri);
}
