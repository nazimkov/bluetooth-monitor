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

    [Fact]
    public void StartupPoll_LoadsSelectedDeviceBattery()
    {
        CaptureOnFailure(
            nameof(StartupPoll_LoadsSelectedDeviceBattery),
            () =>
            {
                App.SetFakeBatteryLevel(18);
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();
                Devices.SelectDevice(FakeBluetoothFacade.EarbudsId);

                UiWait.WaitUntil(
                    () => !string.IsNullOrWhiteSpace(Devices.HeroBatteryPercent()),
                    TimeSpan.FromSeconds(15),
                    "Startup polling did not load the selected device battery level."
                );
            }
        );
    }

    [Fact]
    public void DeviceList_ShowsConnectionDots()
    {
        CaptureOnFailure(
            nameof(DeviceList_ShowsConnectionDots),
            () =>
            {
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();

                UiWait.WaitUntil(
                    () =>
                        Devices.IsConnectedDotVisible(FakeBluetoothFacade.HeadsetId)
                        && Devices.IsDisconnectedDotVisible("e2e-offline"),
                    TimeSpan.FromSeconds(10),
                    "Device list did not show the expected connection dots."
                );
            }
        );
    }

    [Fact]
    public void DisconnectedDevice_ShowsNoBattery()
    {
        CaptureOnFailure(
            nameof(DisconnectedDevice_ShowsNoBattery),
            () =>
            {
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();
                Devices.SelectDevice("e2e-offline");

                UiWait.WaitUntil(
                    () =>
                        Devices.IsHeroDisconnected()
                        && string.IsNullOrWhiteSpace(Devices.HeroBatteryPercent()),
                    TimeSpan.FromSeconds(10),
                    "Disconnected device still showed a battery value."
                );
            }
        );
    }

    [Fact]
    public void Rescan_UpdatesSelectedDeviceBattery()
    {
        CaptureOnFailure(
            nameof(Rescan_UpdatesSelectedDeviceBattery),
            () =>
            {
                App.SetFakeBatteryLevel(61);
                Shell.GoToDevices();
                Devices.WaitUntilLoaded();
                Devices.SelectDevice(FakeBluetoothFacade.HeadsetId);
                Devices.Rescan();

                UiWait.WaitUntil(
                    () => Devices.HeroBatteryPercent() == "61%",
                    TimeSpan.FromSeconds(15),
                    "Rescan did not update the selected device battery level."
                );
            }
        );
    }
}
