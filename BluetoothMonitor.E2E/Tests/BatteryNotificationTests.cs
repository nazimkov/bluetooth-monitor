using BluetoothMonitor.E2E.Fixtures;
using BluetoothMonitor.E2E.Helpers;
using BluetoothMonitor.E2E.TestDoubles;
using Xunit;

namespace BluetoothMonitor.E2E.Tests;

public sealed class BatteryNotificationTests(AppLifecycle app) : E2ETestBase(app)
{
    [Fact]
    public async Task LowBattery_ShowsBannerAndNotification()
    {
        await CaptureOnFailureAsync(
            nameof(LowBattery_ShowsBannerAndNotification),
            async () =>
            {
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();
                UiWait.WaitUntil(
                    () =>
                        Devices
                            .HeroDeviceName()
                            .Contains(
                                FakeBluetoothFacade.EarbudsName,
                                StringComparison.OrdinalIgnoreCase
                            ),
                    TimeSpan.FromSeconds(10),
                    "The low-battery fake device was not selected."
                );

                App.MainWindow.ClickId("HeroRefreshButton");

                UiWait.WaitUntil(
                    () => App.MainWindow.TryById("LowBatteryInfoBar")?.IsOffscreen == false,
                    TimeSpan.FromSeconds(10),
                    "Low-battery banner did not open for the 18% fake device."
                );

                using var notification = await App.WaitForNotificationAsync(
                    root =>
                        root.GetProperty("title").GetString() == "Low battery"
                        && root.GetProperty("message")
                            .GetString()!
                            .Contains("18%", StringComparison.Ordinal),
                    TimeSpan.FromSeconds(10)
                );

                Assert.Equal(
                    "Low battery",
                    notification.RootElement.GetProperty("title").GetString()
                );
                Assert.Contains("18%", notification.RootElement.GetProperty("message").GetString());
            }
        );
    }

    [Fact]
    public async Task LowBatteryBanner_DisappearsWhenBatteryRisesAboveThreshold()
    {
        await CaptureOnFailureAsync(
            nameof(LowBatteryBanner_DisappearsWhenBatteryRisesAboveThreshold),
            () =>
            {
                App.SetFakeBatteryLevel(18);
                Shell.GoToNotifications();
                Notifications.WaitUntilLoaded();
                Notifications.SetLowBatteryThreshold(20);
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();
                App.MainWindow.ClickId("HeroRefreshButton");
                UiWait.WaitUntil(
                    () => App.MainWindow.TryById("LowBatteryInfoBar")?.IsOffscreen == false,
                    TimeSpan.FromSeconds(10),
                    "Low-battery banner did not open."
                );

                App.SetFakeBatteryLevel(26);
                App.MainWindow.ClickId("HeroRefreshButton");
                UiWait.WaitUntil(
                    () => App.MainWindow.TryById("LowBatteryInfoBar")?.IsOffscreen != false,
                    TimeSpan.FromSeconds(10),
                    "Low-battery banner did not close after the battery rose above the threshold."
                );

                return Task.CompletedTask;
            }
        );
    }

    [Fact]
    public async Task LowBatteryBanner_DisappearsWhenThresholdDropsBelowBatteryLevel()
    {
        await CaptureOnFailureAsync(
            nameof(LowBatteryBanner_DisappearsWhenThresholdDropsBelowBatteryLevel),
            () =>
            {
                App.SetFakeBatteryLevel(18);
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();
                App.MainWindow.ClickId("HeroRefreshButton");
                UiWait.WaitUntil(
                    () => App.MainWindow.TryById("LowBatteryInfoBar")?.IsOffscreen == false,
                    TimeSpan.FromSeconds(10),
                    "Low-battery banner did not open."
                );

                Shell.GoToNotifications();
                Notifications.WaitUntilLoaded();
                Notifications.SetLowBatteryThreshold(15);
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();
                UiWait.WaitUntil(
                    () => App.MainWindow.TryById("LowBatteryInfoBar")?.IsOffscreen != false,
                    TimeSpan.FromSeconds(10),
                    "Low-battery banner did not close after the threshold moved below the battery level."
                );

                return Task.CompletedTask;
            }
        );
    }
}
