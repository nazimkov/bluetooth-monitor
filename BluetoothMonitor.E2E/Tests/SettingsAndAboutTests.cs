using BluetoothMonitor.E2E.Fixtures;
using BluetoothMonitor.E2E.Helpers;
using Xunit;

namespace BluetoothMonitor.E2E.Tests;

public sealed class SettingsAndAboutTests(AppLifecycle app) : E2ETestBase(app)
{
    [Fact]
    public async Task GeneralSettings_RefreshIntervalAndBackgroundMode_Persist()
    {
        await CaptureOnFailureAsync(
            nameof(GeneralSettings_RefreshIntervalAndBackgroundMode_Persist),
            async () =>
            {
                Shell.GoToGeneral();
                General.WaitUntilLoaded();

                General.SelectRefreshIntervalIndex(0);
                General.ToggleKeepRunning();

                await App.WaitForSettingsValueAsync(root =>
                    root.GetProperty("refreshIntervalSeconds").GetInt32() == 15
                    && root.TryGetProperty("keepRunningInBackground", out var keepRunning)
                    && !keepRunning.GetBoolean()
                );

                Assert.Equal("Every 15 seconds", General.SelectedRefreshIntervalText());
            }
        );
    }

    [Fact]
    public async Task NotificationSettings_StyleAndCriticalAlert_Persist()
    {
        await CaptureOnFailureAsync(
            nameof(NotificationSettings_StyleAndCriticalAlert_Persist),
            async () =>
            {
                Shell.GoToNotifications();
                Notifications.WaitUntilLoaded();

                Notifications.SelectNotificationStyleIndex(3);
                Notifications.ToggleCriticalAlert();

                await App.WaitForSettingsValueAsync(root =>
                    root.GetProperty("notificationStyle").GetString() == "silent"
                    && root.TryGetProperty("criticalAlertUnder5", out var criticalAlert)
                    && !criticalAlert.GetBoolean()
                );

                Assert.Equal("Silent", Notifications.SelectedNotificationStyleText());
            }
        );
    }

    [Fact]
    public void DndSilencing_IsDisabledWithExplanation()
    {
        CaptureOnFailure(
            nameof(DndSilencing_IsDisabledWithExplanation),
            () =>
            {
                Shell.GoToNotifications();
                Notifications.WaitUntilLoaded();

                Assert.False(Notifications.IsDndSilencingEnabled());
            }
        );
    }

    [Fact]
    public void About_CheckForUpdates_ShowsAvailableRelease()
    {
        CaptureOnFailure(
            nameof(About_CheckForUpdates_ShowsAvailableRelease),
            () =>
            {
                Shell.GoToAbout();
                App.MainWindow.ClickId("CheckForUpdatesButton");

                UiWait.WaitUntil(
                    () => App.MainWindow.TryById("UpdateInfoBar")?.IsOffscreen == false,
                    TimeSpan.FromSeconds(5),
                    "The update result was not shown."
                );

                Assert.NotNull(
                    App.MainWindow.FindFirstDescendant(cf =>
                        cf.ByName("Version v9.9.0-beta.1 is available.")
                    )
                );
                Assert.NotNull(App.MainWindow.TryById("OpenUpdateButton"));
            }
        );
    }
}
