using System.Reflection;
using Serilog;

namespace BluetoothMonitor.App.Services;

internal static class AppLogging
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;

        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BluetoothMonitor",
            "Logs"
        );
        Directory.CreateDirectory(logDirectory);

        var version =
            Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion?.Split('+')[0]
            ?? "unknown";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("Application", "BluetoothMonitor")
            .Enrich.WithProperty("ApplicationVersion", version)
            .WriteTo.Debug()
            .WriteTo.File(
                Path.Combine(logDirectory, "app-.log"),
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                retainedFileCountLimit: 14,
                shared: true
            )
            .CreateLogger();

        _initialized = true;
    }

    public static void CloseAndFlush() => Log.CloseAndFlush();
}
