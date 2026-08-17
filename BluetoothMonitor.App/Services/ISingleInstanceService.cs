namespace BluetoothMonitor.App.Services;

public interface ISingleInstanceService
{
    bool RedirectIfNotPrimary();
}
