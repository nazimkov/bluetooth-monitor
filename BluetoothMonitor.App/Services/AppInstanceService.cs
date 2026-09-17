using BluetoothMonitor.App.Services.Test;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppLifecycle;

namespace BluetoothMonitor.App.Services;

public sealed class AppInstanceService : ISingleInstanceService
{
    private readonly ILogger<AppInstanceService> _logger;

    public AppInstanceService(ILogger<AppInstanceService> logger) => _logger = logger;

    public bool RedirectIfNotPrimary()
    {
        var args = AppInstance.GetCurrent().GetActivatedEventArgs();
        var key = E2ETestHost.IsEnabled ? E2ETestHost.InstanceKey : "BluetoothMonitor.App";
        var instance = AppInstance.FindOrRegisterForKey(key);
        if (instance.IsCurrent)
        {
            instance.Activated += (_, e) =>
            {
                App.Current.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                    App.Current.MainWindow?.ShowWindow()
                );
            };
            return false;
        }
        instance.RedirectActivationToAsync(args).AsTask().GetAwaiter().GetResult();
        _logger.LogInformation("Activation redirected to the primary instance");
        return true;
    }
}
