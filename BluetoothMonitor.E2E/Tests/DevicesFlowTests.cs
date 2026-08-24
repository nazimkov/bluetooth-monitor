using BluetoothMonitor.E2E.Fixtures;
using BluetoothMonitor.E2E.Helpers;
using Xunit;

namespace BluetoothMonitor.E2E.Tests;

public sealed class DevicesFlowTests(AppLifecycle app) : E2ETestBase(app)
{
    [Fact]
    public void SelectDevice_UpdatesHero()
    {
        CaptureOnFailure(
            nameof(SelectDevice_UpdatesHero),
            () =>
            {
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();

                Devices.SelectDevice("e2e-earbuds");
                UiWait.WaitUntil(
                    () =>
                        Devices
                            .HeroDeviceName()
                            .Contains("E2E Earbuds", StringComparison.OrdinalIgnoreCase),
                    TimeSpan.FromSeconds(10),
                    $"Hero did not show E2E Earbuds (was '{Devices.HeroDeviceName()}')."
                );

                Devices.SelectDevice("e2e-headset");
                UiWait.WaitUntil(
                    () =>
                        Devices
                            .HeroDeviceName()
                            .Contains("E2E Headset", StringComparison.OrdinalIgnoreCase),
                    TimeSpan.FromSeconds(10),
                    $"Hero did not show E2E Headset (was '{Devices.HeroDeviceName()}')."
                );
            }
        );
    }

    [Fact]
    public void SelectDevice_SurvivesTabSwitch()
    {
        CaptureOnFailure(
            nameof(SelectDevice_SurvivesTabSwitch),
            () =>
            {
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();

                Devices.SelectDevice("e2e-earbuds");
                UiWait.WaitUntil(
                    () =>
                        Devices
                            .HeroDeviceName()
                            .Contains("E2E Earbuds", StringComparison.OrdinalIgnoreCase),
                    TimeSpan.FromSeconds(10),
                    $"Hero did not show E2E Earbuds (was '{Devices.HeroDeviceName()}')."
                );

                Shell.GoToNotifications();
                Notifications.WaitUntilLoaded();

                Shell.GoToDevices();
                Devices.WaitUntilLoaded();

                UiWait.WaitUntil(
                    () =>
                        Devices
                            .HeroDeviceName()
                            .Contains("E2E Earbuds", StringComparison.OrdinalIgnoreCase),
                    TimeSpan.FromSeconds(10),
                    $"Selection was lost after switching tabs (hero was '{Devices.HeroDeviceName()}')."
                );
            }
        );
    }

    [Fact]
    public void Rescan_KeepsFakeDevices()
    {
        CaptureOnFailure(
            nameof(Rescan_KeepsFakeDevices),
            () =>
            {
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();
                Devices.Rescan();

                UiWait.WaitUntil(
                    () => Devices.HasDevice("e2e-headset") && Devices.HasDevice("e2e-earbuds"),
                    TimeSpan.FromSeconds(15),
                    "Fake devices missing after Rescan."
                );
            }
        );
    }
}
