using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel;

namespace BluetoothMonitor.App.Services;

public sealed class StartupTaskService : IStartupService
{
    private const string TaskId = "BluetoothMonitorStartup";
    private readonly ILogger<StartupTaskService> _logger;

    public StartupTaskService(ILogger<StartupTaskService> logger) => _logger = logger;

    public async Task<bool> IsEnabledAsync()
    {
        var task = await StartupTask.GetAsync(TaskId);
        _logger.LogDebug("Startup task state is {StartupTaskState}", task.State);
        return task.State == StartupTaskState.Enabled;
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        var task = await StartupTask.GetAsync(TaskId);
        if (enabled)
        {
            if (task.State is StartupTaskState.Disabled)
            {
                await task.RequestEnableAsync();
                _logger.LogInformation("Startup task enabled");
            }
        }
        else
        {
            if (task.State is StartupTaskState.Enabled)
            {
                task.Disable();
                _logger.LogInformation("Startup task disabled");
            }
        }
    }
}
