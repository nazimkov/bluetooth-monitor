using Microsoft.Windows.ApplicationModel.Resources;

namespace BluetoothMonitor.App.Services;

internal static class AppResources
{
    private static readonly ResourceLoader Loader = new ResourceLoader();

    public static string Get(string key) => Loader.GetString(key.Replace(".", "/"));
}
