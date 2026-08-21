using BluetoothMonitor.E2E.Fixtures;
using BluetoothMonitor.E2E.Helpers;
using BluetoothMonitor.E2E.Pages;
using Xunit;

namespace BluetoothMonitor.E2E.Tests;

[Collection("E2E")]
public abstract class E2ETestBase(AppLifecycle app)
{
    protected AppLifecycle App { get; } = app;
    protected MainShell Shell => new(App.MainWindow);
    protected DevicesPage Devices => new(App.MainWindow);
    protected NotificationsPage Notifications => new(App.MainWindow);
    protected GeneralPage General => new(App.MainWindow);

    protected void CaptureOnFailure(string testName, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            var path = FailureArtifacts.Write(
                App.RunId,
                testName,
                ex.Message,
                App.MainWindow,
                ex);
            throw new Xunit.Sdk.XunitException($"{ex.Message}\n\nArtifacts: {path}");
        }
    }

    protected async Task CaptureOnFailureAsync(string testName, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            var path = FailureArtifacts.Write(
                App.RunId,
                testName,
                ex.Message,
                App.MainWindow,
                ex);
            throw new Xunit.Sdk.XunitException($"{ex.Message}\n\nArtifacts: {path}");
        }
    }
}
