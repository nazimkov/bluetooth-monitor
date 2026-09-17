using BluetoothMonitor.E2E.Fixtures;
using BluetoothMonitor.E2E.Helpers;
using Xunit;

namespace BluetoothMonitor.E2E.Tests;

public sealed class MissingCoverageTests(AppLifecycle app) : E2ETestBase(app)
{
    [Fact]
    public async Task CriticalBattery_ShowsBannerAndNotification()
    {
        await CaptureOnFailureAsync(
            nameof(CriticalBattery_ShowsBannerAndNotification),
            async () =>
            {
                Shell.GoToNotifications();
                Notifications.WaitUntilLoaded();
                if (!Notifications.IsCriticalAlertOn())
                    Notifications.ToggleCriticalAlert();

                App.SetFakeBatteryLevel(4);
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();
                App.MainWindow.ClickId("HeroRefreshButton");

                UiWait.WaitUntil(
                    () => App.MainWindow.TryById("CriticalBatteryInfoBar")?.IsOffscreen == false,
                    TimeSpan.FromSeconds(10),
                    "Critical-battery banner did not open for the 4% fake device."
                );

                using var notification = await App.WaitForNotificationAsync(
                    root =>
                        root.GetProperty("title").GetString() == "Critical battery"
                        && root.GetProperty("message").GetString()!.Contains("4%"),
                    TimeSpan.FromSeconds(10)
                );

                Assert.Contains("4%", notification.RootElement.GetProperty("message").GetString());
            }
        );
    }

    [Fact]
    public void CriticalBattery_DisabledAlert_DoesNotShowCriticalBanner()
    {
        CaptureOnFailure(
            nameof(CriticalBattery_DisabledAlert_DoesNotShowCriticalBanner),
            () =>
            {
                Shell.GoToNotifications();
                Notifications.WaitUntilLoaded();
                if (Notifications.IsCriticalAlertOn())
                    Notifications.ToggleCriticalAlert();

                App.SetFakeBatteryLevel(4);
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();
                App.MainWindow.ClickId("HeroRefreshButton");

                UiWait.WaitUntil(
                    () => App.MainWindow.TryById("CriticalBatteryInfoBar")?.IsOffscreen != false,
                    TimeSpan.FromSeconds(10),
                    "Critical-battery banner opened while the critical alert was disabled."
                );
            }
        );
    }

    [Fact]
    public async Task StartAtSignIn_PersistsToSettingsJson()
    {
        await CaptureOnFailureAsync(
            nameof(StartAtSignIn_PersistsToSettingsJson),
            async () =>
            {
                Shell.GoToGeneral();
                General.WaitUntilLoaded();
                var expected = !General.IsStartAtSignInOn();
                General.ToggleStartAtSignIn();

                await App.WaitForSettingsValueAsync(root =>
                    root.GetProperty("startAtSignIn").GetBoolean() == expected
                );
            }
        );
    }

    [Fact]
    public async Task ThemeSelection_PersistsToSettingsJson()
    {
        await CaptureOnFailureAsync(
            nameof(ThemeSelection_PersistsToSettingsJson),
            async () =>
            {
                Shell.GoToGeneral();
                General.WaitUntilLoaded();
                General.SelectThemeIndex(1);

                await App.WaitForSettingsValueAsync(root =>
                    root.GetProperty("theme").GetString() == "light"
                );
            }
        );
    }
}
