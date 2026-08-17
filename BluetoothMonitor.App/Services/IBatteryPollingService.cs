using System;
using System.Threading;
using System.Threading.Tasks;

namespace BluetoothMonitor.App.Services;

public sealed class BatteryUpdatedEventArgs : EventArgs
{
    public required string DeviceId { get; init; }
    public required byte? Level { get; init; }
}

public interface IBatteryPollingService
{
    event EventHandler<BatteryUpdatedEventArgs>? BatteryUpdated;
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task PollOnceAsync();
}
