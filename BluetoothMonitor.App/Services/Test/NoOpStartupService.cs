using System.Threading.Tasks;

namespace BluetoothMonitor.App.Services.Test;

public sealed class NoOpStartupService : IStartupService
{
    private bool _enabled;

    public Task<bool> IsEnabledAsync() => Task.FromResult(_enabled);

    public Task SetEnabledAsync(bool enabled)
    {
        _enabled = enabled;
        return Task.CompletedTask;
    }
}
