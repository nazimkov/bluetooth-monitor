using BluetoothMonitor.E2E.Helpers;
using BluetoothMonitor.E2E.TestDoubles;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace BluetoothMonitor.E2E.Pages;

public sealed class MainShell(Window window)
{
    public Window Window { get; } = window;

    public void GoToDevices() => Navigate("NavDevices", "DevicesPageTitle");

    public void GoToNotifications() => Navigate("NavNotifications", "NotificationsPageTitle");

    public void GoToGeneral() => Navigate("NavGeneral", "GeneralPageTitle");

    public void GoToAbout() => Navigate("NavAbout", "AboutPageTitle");

    private void Navigate(string navId, string pageTitleId)
    {
        var item = Window.ById(navId);
        try
        {
            if (item.Patterns.SelectionItem.IsSupported)
            {
                item.Patterns.SelectionItem.Pattern.Select();
            }
            else
            {
                item.Click();
            }
        }
        catch
        {
            item.Click();
        }

        Window.ById(pageTitleId);
    }

    public void AssertPageTitle(string automationId, string expectedText)
    {
        var title = Window.ById(automationId);
        UiWait.WaitUntil(
            () => string.Equals(title.Name, expectedText, StringComparison.Ordinal),
            TimeSpan.FromSeconds(10),
            $"Page title '{automationId}' did not become '{expectedText}' (was '{title.Name}')."
        );
    }
}

public sealed class DevicesPage(Window window)
{
    public Window Window { get; } = window;

    public void WaitUntilLoaded()
    {
        Window.ById("DevicesPageTitle");
        UiWait.WaitUntil(
            () =>
                HasDevice(FakeBluetoothFacade.HeadsetId)
                || Window.TryById("EmptyDevicesState") is not null
                || Window.FindFirstDescendant(cf => cf.ByName(FakeBluetoothFacade.HeadsetName))
                    is not null,
            TimeSpan.FromSeconds(20),
            "Devices page did not show a device list or empty state."
        );
    }

    public void Rescan() => Window.ClickId("RescanButton");

    public void SelectDevice(string deviceId)
    {
        var row =
            FindDeviceElement(deviceId)
            ?? throw new InvalidOperationException($"Device '{deviceId}' not found in UI.");

        if (row.Patterns.ScrollItem.IsSupported)
        {
            row.Patterns.ScrollItem.Pattern.ScrollIntoView();
        }

        row.Focus();
        (Window.TryById($"DeviceName_{deviceId}") ?? row).Click();
    }

    public string HeroDeviceName()
    {
        var name = Window.ById("HeroDeviceName");
        return name.Name;
    }

    public string HeroBatteryPercent() => Window.TryById("BatteryPercent")?.Name ?? string.Empty;

    public bool IsHeroConnected() => Window.TryById("HeroConnectedStatus") is not null;

    public bool IsHeroDisconnected() => Window.TryById("HeroDisconnectedStatus") is not null;

    public bool HasDevice(string deviceId) => FindDeviceElement(deviceId) is not null;

    public bool IsConnectedDotVisible(string deviceId) => IsVisible($"ConnectedDot_{deviceId}");

    public bool IsDisconnectedDotVisible(string deviceId) =>
        IsVisible($"DisconnectedDot_{deviceId}");

    private bool IsVisible(string automationId) => Window.TryById(automationId) is not null;

    private AutomationElement? FindDeviceElement(string deviceId)
    {
        return Window.TryById($"DeviceRow_{deviceId}")
            ?? Window.TryById($"DeviceName_{deviceId}")
            ?? deviceId switch
            {
                FakeBluetoothFacade.HeadsetId => Window.FindFirstDescendant(cf =>
                    cf.ByName(FakeBluetoothFacade.HeadsetName)
                ),
                FakeBluetoothFacade.EarbudsId => Window.FindFirstDescendant(cf =>
                    cf.ByName(FakeBluetoothFacade.EarbudsName)
                ),
                _ => null,
            };
    }
}

public sealed class NotificationsPage(Window window)
{
    public Window Window { get; } = window;

    public void WaitUntilLoaded() => Window.ById("NotificationsPageTitle");

    public void SetLowBatteryThreshold(double value)
    {
        var slider = Window.ById("LowBatteryThresholdSlider").AsSlider();
        slider.Value = value;
        // Nudge to ensure TwoWay binding commits
        Keyboard.Press(VirtualKeyShort.TAB);
        Thread.Sleep(100);
    }

    public double GetLowBatteryThreshold()
    {
        var slider = Window.ById("LowBatteryThresholdSlider").AsSlider();
        return slider.Value;
    }

    public void SelectNotificationStyleIndex(int index)
    {
        var combo = Window.ById("NotificationStyleCombo").AsComboBox();
        combo.Select(index);
        Thread.Sleep(200);
    }

    public string SelectedNotificationStyleText()
    {
        var combo = Window.ById("NotificationStyleCombo").AsComboBox();
        return combo.SelectedItem?.Text ?? string.Empty;
    }

    public void SelectAlertSoundIndex(int index)
    {
        var combo = Window.ById("AlertSoundCombo").AsComboBox();
        combo.Select(index);
        Thread.Sleep(200);
    }

    public string SelectedAlertSoundText()
    {
        var combo = Window.ById("AlertSoundCombo").AsComboBox();
        return combo.SelectedItem?.Text ?? string.Empty;
    }

    public void PreviewAlertSound() => Window.ClickId("PreviewSoundButton");

    public void ToggleCriticalAlert() => Window.ClickId("CriticalAlertToggle");

    public bool IsDndSilencingEnabled() => Window.ById("SilenceDndToggle").IsEnabled;
}

public sealed class GeneralPage(Window window)
{
    public Window Window { get; } = window;

    public void WaitUntilLoaded() => Window.ById("GeneralPageTitle");

    public void SelectThemeIndex(int index)
    {
        var combo = Window.ById("ThemeCombo").AsComboBox();
        combo.Select(index);
        Thread.Sleep(200);
    }

    public string SelectedThemeText()
    {
        var combo = Window.ById("ThemeCombo").AsComboBox();
        return combo.SelectedItem?.Text ?? string.Empty;
    }

    public void ToggleStartAtSignIn() => Window.ClickId("StartAtSignInToggle");

    public void ToggleKeepRunning() => Window.ClickId("KeepRunningToggle");

    public void SelectRefreshIntervalIndex(int index)
    {
        var combo = Window.ById("RefreshIntervalCombo").AsComboBox();
        combo.Select(index);
        Thread.Sleep(200);
    }

    public string SelectedRefreshIntervalText()
    {
        var combo = Window.ById("RefreshIntervalCombo").AsComboBox();
        return combo.SelectedItem?.Text ?? string.Empty;
    }
}
