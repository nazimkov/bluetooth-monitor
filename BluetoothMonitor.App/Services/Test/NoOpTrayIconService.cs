namespace BluetoothMonitor.App.Services.Test;

public sealed class NoOpTrayIconService : ITrayIconService
{
    public void Initialize() { }
    public void UpdateIcon(byte? level, bool connected) { }
    public void Dispose() { }
}
