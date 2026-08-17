namespace BluetoothMonitor.App.Services;

public interface ITrayIconService
{
    void Initialize();
    void UpdateIcon(byte? level, bool connected);
    void Dispose();
}
