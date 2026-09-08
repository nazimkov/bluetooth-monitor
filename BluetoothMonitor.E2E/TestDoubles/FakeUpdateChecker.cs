namespace BluetoothMonitor.E2E.TestDoubles;

public sealed class FakeUpdateChecker
{
    public Task<FakeUpdateResult> CheckForUpdateAsync(string currentVersion) =>
        Task.FromResult(
            new FakeUpdateResult(
                IsUpdateAvailable: true,
                LatestVersion: "v9.9.0-beta.1",
                ReleaseUrl: "https://github.com/nazimkov/bluetooth-battery-hawk/releases/tag/v9.9.0-beta.1"
            )
        );
}

public sealed record FakeUpdateResult(
    bool IsUpdateAvailable,
    string LatestVersion,
    string ReleaseUrl
);
