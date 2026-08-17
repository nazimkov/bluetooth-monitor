using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace BluetoothMonitor.App.Services;

public sealed class StartupTaskService : IStartupService
{
    private const string TaskId = "BattcheckStartup";

    public async Task<bool> IsEnabledAsync()
    {
        var task = await StartupTask.GetAsync(TaskId);
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
            }
        }
        else
        {
            if (task.State is StartupTaskState.Enabled)
            {
                task.Disable();
            }
        }
    }
}
