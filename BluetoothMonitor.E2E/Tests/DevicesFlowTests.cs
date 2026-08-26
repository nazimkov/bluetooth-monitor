using BluetoothMonitor.E2E.Fixtures;
using BluetoothMonitor.E2E.Helpers;
using BluetoothMonitor.E2E.TestDoubles;
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

                Devices.SelectDevice(FakeBluetoothFacade.EarbudsId);
                UiWait.WaitUntil(
                    () =>
                        Devices
                            .HeroDeviceName()
                            .Contains(
                                FakeBluetoothFacade.EarbudsName,
                                StringComparison.OrdinalIgnoreCase
                            ),
                    TimeSpan.FromSeconds(10),
                    $"Hero did not show {FakeBluetoothFacade.EarbudsName} (was '{Devices.HeroDeviceName()}')."
                );

                Devices.SelectDevice(FakeBluetoothFacade.HeadsetId);
                UiWait.WaitUntil(
                    () =>
                        Devices
                            .HeroDeviceName()
                            .Contains(
                                FakeBluetoothFacade.HeadsetName,
                                StringComparison.OrdinalIgnoreCase
                            ),
                    TimeSpan.FromSeconds(10),
                    $"Hero did not show {FakeBluetoothFacade.HeadsetName} (was '{Devices.HeroDeviceName()}')."
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

                Devices.SelectDevice(FakeBluetoothFacade.EarbudsId);
                UiWait.WaitUntil(
                    () =>
                        Devices
                            .HeroDeviceName()
                            .Contains(
                                FakeBluetoothFacade.EarbudsName,
                                StringComparison.OrdinalIgnoreCase
                            ),
                    TimeSpan.FromSeconds(10),
                    $"Hero did not show {FakeBluetoothFacade.EarbudsName} (was '{Devices.HeroDeviceName()}')."
                );

                Shell.GoToNotifications();
                Notifications.WaitUntilLoaded();

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
                    () =>
                        Devices.HasDevice(FakeBluetoothFacade.HeadsetId)
                        && Devices.HasDevice(FakeBluetoothFacade.EarbudsId),
                    TimeSpan.FromSeconds(15),
                    "Fake devices missing after Rescan."
                );
            }
        );
    }
}
