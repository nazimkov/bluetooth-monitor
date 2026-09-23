using BluetoothMonitor.E2E.Fixtures;
using BluetoothMonitor.E2E.Helpers;
using Xunit;

namespace BluetoothMonitor.E2E.Tests;

[Collection("E2E")]
public sealed class SearchSettingsTests(AppLifecycle app) : E2ETestBase(app)
{
    [Fact]
    public void SearchBox_IsEnabled()
    {
        CaptureOnFailure(
            nameof(SearchBox_IsEnabled),
            () => Assert.True(Shell.IsSearchEnabled(), "The settings search box is disabled.")
        );
    }

    [Fact]
    public void SearchSettings_SelectingSuggestion_OpensMatchingPage()
    {
        CaptureOnFailure(
            nameof(SearchSettings_SelectingSuggestion_OpensMatchingPage),
            () =>
            {
                try
                {
                    Shell.GoToDevices();
                    Shell.Search("theme");

                    var suggestion = Shell.WaitForSearchSuggestion("Theme");
                    Assert.True(
                        suggestion.IsEnabled,
                        "The matching search suggestion is disabled."
                    );

                    Shell.ChooseSearchSuggestion("Theme");
                    General.WaitUntilLoaded();
                }
                finally
                {
                    Shell.ClearSearch();
                }
            }
        );
    }

    [Fact]
    public void SearchSettings_SubmittingQuery_OpensMatchingPage()
    {
        CaptureOnFailure(
            nameof(SearchSettings_SubmittingQuery_OpensMatchingPage),
            () =>
            {
                try
                {
                    Shell.GoToDevices();
                    Shell.Search("notification style");
                    Shell.SubmitSearch();
                    Shell.ChooseSearchSuggestion("Notification style");

                    Notifications.WaitUntilLoaded();
                }
                finally
                {
                    Shell.ClearSearch();
                }
            }
        );
    }
}
