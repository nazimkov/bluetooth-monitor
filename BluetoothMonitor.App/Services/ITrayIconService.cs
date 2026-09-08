namespace BluetoothMonitor.App.Services;

public interface ITrayIconService : IDisposable
{
    void Initialize();
    void UpdateIcon(byte? level, bool connected);
}
