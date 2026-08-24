using BluetoothMonitor.E2E.Fixtures;
using BluetoothMonitor.E2E.Helpers;
using Xunit;

namespace BluetoothMonitor.E2E.Tests;

public sealed class GeneralSettingsTests(AppLifecycle app) : E2ETestBase(app)
{
    [Fact]
    public void ThemeCombo_CanSelectLight()
    {
        CaptureOnFailure(
            nameof(ThemeCombo_CanSelectLight),
            () =>
            {
                Shell.GoToGeneral();
                General.WaitUntilLoaded();

                General.SelectThemeIndex(1); // Light
                UiWait.WaitUntil(
                    () =>
                        string.Equals(
                            General.SelectedThemeText(),
                            "Light",
                            StringComparison.OrdinalIgnoreCase
                        ),
                    TimeSpan.FromSeconds(5),
                    $"Theme combo was '{General.SelectedThemeText()}', expected Light."
                );
            }
        );
    }
}
