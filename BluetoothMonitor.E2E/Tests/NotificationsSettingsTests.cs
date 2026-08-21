using BluetoothMonitor.E2E.Fixtures;
using Xunit;

namespace BluetoothMonitor.E2E.Tests;

public sealed class NotificationsSettingsTests(AppLifecycle app) : E2ETestBase(app)
{
    [Fact]
    public async Task LowBatteryThreshold_PersistsToSettingsJson()
    {
        await CaptureOnFailureAsync(nameof(LowBatteryThreshold_PersistsToSettingsJson), async () =>
        {
            Shell.GoToNotifications();
            Notifications.WaitUntilLoaded();

            Notifications.SetLowBatteryThreshold(35);

            await App.WaitForSettingsValueAsync(root =>
                root.TryGetProperty("lowBatteryThreshold", out var prop)
                && prop.GetInt32() == 35);

            Assert.Equal(35, Notifications.GetLowBatteryThreshold(), 0.1);
        });
    }
}
