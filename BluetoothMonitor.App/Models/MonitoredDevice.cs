namespace BluetoothMonitor.App.Models;

public record MonitoredDevice(
    string Id,
    string Name,
    DeviceKind Kind,
    string? MacAddress,
    bool IsConnected,
    byte? BatteryLevel,
    string LastSeen);
