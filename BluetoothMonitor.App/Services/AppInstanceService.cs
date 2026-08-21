using BluetoothMonitor.App.Services.Test;
using Microsoft.Windows.AppLifecycle;

namespace BluetoothMonitor.App.Services;

public sealed class AppInstanceService : ISingleInstanceService
{
    public bool RedirectIfNotPrimary()
    {
        var args = AppInstance.GetCurrent().GetActivatedEventArgs();
        var key = E2ETestHost.IsEnabled
            ? E2ETestHost.InstanceKey
            : "BluetoothMonitor.App";
        var instance = AppInstance.FindOrRegisterForKey(key);
        if (instance.IsCurrent)
        {
            instance.Activated += (_, e) =>
            {
                App.Current.MainWindow?.DispatcherQueue.TryEnqueue(() => App.Current.MainWindow?.ShowWindow());
            };
            return false;
        }
        instance.RedirectActivationToAsync(args).AsTask().GetAwaiter().GetResult();
        return true;
    }
}
