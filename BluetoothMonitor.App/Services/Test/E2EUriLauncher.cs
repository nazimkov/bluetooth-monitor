namespace BluetoothMonitor.App.Services.Test;

internal sealed class E2EUriLauncher : IUriLauncher
{
    private readonly string _launchesPath;

    public E2EUriLauncher(string launchesPath)
    {
        _launchesPath = launchesPath;
    }

    public Task<bool> LaunchAsync(Uri uri)
    {
        File.AppendAllText(_launchesPath, uri + Environment.NewLine);
        return Task.FromResult(true);
    }
}
