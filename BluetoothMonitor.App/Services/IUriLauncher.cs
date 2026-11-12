namespace BluetoothMonitor.App.Services;

public interface IUriLauncher
{
    Task<bool> LaunchAsync(Uri uri);
}
