using BluetoothMonitor.E2E.Fixtures;
using BluetoothMonitor.E2E.Helpers;
using BluetoothMonitor.E2E.TestDoubles;
using Xunit;

namespace BluetoothMonitor.E2E.Tests;

public sealed class SmokeNavigationTests(AppLifecycle app) : E2ETestBase(app)
{
    [Fact]
    public void ColdStart_OpensDevices_WithFakeDevices()
    {
        CaptureOnFailure(
            nameof(ColdStart_OpensDevices_WithFakeDevices),
            () =>
            {
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();
                Shell.AssertPageTitle("DevicesPageTitle", "Devices");
                Assert.True(
                    Devices.HasDevice(FakeBluetoothFacade.HeadsetId),
                    $"Expected DeviceRow_{FakeBluetoothFacade.HeadsetId} in the list."
                );
                Assert.True(
                    Devices.HasDevice(FakeBluetoothFacade.EarbudsId),
                    $"Expected DeviceRow_{FakeBluetoothFacade.EarbudsId} in the list."
                );
            }
        );
    }

    [Fact]
    public void Navigate_AllSidebarPages()
    {
        CaptureOnFailure(
            nameof(Navigate_AllSidebarPages),
            () =>
            {
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();

                Shell.GoToNotifications();
                Notifications.WaitUntilLoaded();
                Shell.AssertPageTitle("NotificationsPageTitle", "Notifications");

                Shell.GoToGeneral();
                General.WaitUntilLoaded();
                Shell.AssertPageTitle("GeneralPageTitle", "General");

                Shell.GoToAbout();
                App.MainWindow.ById("AboutPageTitle");
                Shell.AssertPageTitle("AboutPageTitle", "About Battcheck");

                Shell.GoToDevices();
                Devices.WaitUntilLoaded();
                Shell.AssertPageTitle("DevicesPageTitle", "Devices");
            }
        );
    }
}
