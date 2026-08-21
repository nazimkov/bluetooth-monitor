using System;
using BluetoothMonitor.App.Services.Test;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using WinRT;

namespace BluetoothMonitor.App;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();

        var isRedirect = DecideRedirection();
        if (isRedirect)
        {
            return 0;
        }

        Microsoft.UI.Xaml.Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });

        return 0;
    }

    private static bool DecideRedirection()
    {
        var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        var key = E2ETestHost.IsEnabled
            ? E2ETestHost.InstanceKey
            : "BluetoothMonitor.App";
        var instance = AppInstance.FindOrRegisterForKey(key);

        if (instance.IsCurrent)
        {
            return false;
        }

        instance.RedirectActivationToAsync(activatedArgs).AsTask().GetAwaiter().GetResult();
        return true;
    }
}
