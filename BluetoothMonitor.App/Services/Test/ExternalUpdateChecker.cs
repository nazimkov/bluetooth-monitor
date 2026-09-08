using BluetoothMonitor.App.Services;

namespace BluetoothMonitor.App.Services.Test;

internal sealed class ExternalUpdateChecker(object inner) : IUpdateChecker
{
    public async Task<UpdateCheckResult> CheckForUpdateAsync(
        string currentVersion,
        CancellationToken cancellationToken = default
    )
    {
        var method = inner.GetType().GetMethod(nameof(CheckForUpdateAsync))!;
        var task = (Task)method.Invoke(inner, [currentVersion])!;
        await task;
        var value = task.GetType().GetProperty("Result")!.GetValue(task)!;
        var type = value.GetType();
        var url = (string?)type.GetProperty("ReleaseUrl")?.GetValue(value);
        return new(
            (bool)type.GetProperty("IsUpdateAvailable")!.GetValue(value)!,
            (string?)type.GetProperty("LatestVersion")?.GetValue(value),
            Uri.TryCreate(url, UriKind.Absolute, out var parsed) ? parsed : null
        );
    }
}
