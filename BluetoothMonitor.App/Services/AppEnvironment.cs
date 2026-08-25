using Windows.ApplicationModel;

namespace BluetoothMonitor.App.Services;

internal static class AppEnvironment
{
    public static bool IsPackaged
    {
        get
        {
            try
            {
                _ = Package.Current.Id;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}
