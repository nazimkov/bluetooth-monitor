using System.Threading.Tasks;

namespace BluetoothMonitor.App.Services;

public interface IStartupService
{
    Task<bool> IsEnabledAsync();
    Task SetEnabledAsync(bool enabled);
}
