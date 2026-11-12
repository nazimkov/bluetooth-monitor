using System;
using System.IO;
using System.Reflection;
using BluetoothMonitor.App.Services;
using BluetoothMonitor.App.Services.Test;
using BluetoothMonitor.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Serilog;
using WinRT;

namespace BluetoothMonitor.App;

public partial class App : Application
{
    public static new App Current => (App)Application.Current;
    public static DispatcherQueue UiDispatcherQueue { get; private set; } = null!;
    public IServiceProvider Services { get; private set; } = null!;
    public MainWindow? MainWindow { get; private set; }

    public App()
    {
        AppLogging.Initialize();
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var logger = Log.Logger.ForContext<App>();
        logger.Information("Application starting");
        UiDispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Services = ConfigureServices();

        var settings = Services.GetRequiredService<ISettingsService>();
        await settings.LoadAsync();
        if (E2ETestHost.IsEnabled && E2ETestHost.SelectedDeviceId is { Length: > 0 } selectedId)
        {
            settings.Update(current => current.SelectedDeviceId = selectedId);
        }

        if (!E2ETestHost.IsEnabled && AppEnvironment.IsPackaged)
        {
            AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
            AppNotificationManager.Default.Register();
        }

        var instance = Services.GetRequiredService<ISingleInstanceService>();
        _ = instance.RedirectIfNotPrimary();

        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Activate();

        var polling = Services.GetRequiredService<IBatteryPollingService>();
        _ = polling.StartAsync(default);

        var tray = Services.GetRequiredService<ITrayIconService>();
        tray.Initialize();
        _ = polling.PollOnceAsync();
        logger.Information("Application initialization completed");
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.AddSerilog(Log.Logger, dispose: false));
        var e2e = E2ETestHost.IsEnabled;
        if (e2e && E2ETestHost.UriLaunchesPath is { Length: > 0 } uriLaunchesPath)
        {
            _ = services.AddSingleton<IUriLauncher>(_ => new E2EUriLauncher(uriLaunchesPath));
        }
        else
        {
            _ = services.AddSingleton<IUriLauncher, WindowsUriLauncher>();
        }

        if (e2e && E2ETestHost.SettingsDirectory is { Length: > 0 } settingsDir)
        {
            _ = services.AddSingleton<ISettingsService>(_ => new JsonSettingsService(settingsDir));
        }
        else
        {
            _ = services.AddSingleton<ISettingsService, JsonSettingsService>();
        }

        if (e2e)
        {
            _ = services.AddSingleton<IBluetoothFacade>(_ => LoadE2EFacade());
            _ = services.AddSingleton<INotificationService, E2ENotificationService>();
            _ = services.AddSingleton<IStartupService, NoOpStartupService>();
            _ = services.AddSingleton<ITrayIconService, NoOpTrayIconService>();
        }
        else if (AppEnvironment.IsPackaged)
        {
            _ = services.AddSingleton<IBluetoothFacade, BluetoothFacade>();
            _ = services.AddSingleton<INotificationService, AppNotificationService>();
            _ = services.AddSingleton<IStartupService, StartupTaskService>();
            _ = services.AddSingleton<ITrayIconService, TrayIconService>();
        }
        else
        {
            _ = services.AddSingleton<IBluetoothFacade, BluetoothFacade>();
            _ = services.AddSingleton<INotificationService, NoOpNotificationService>();
            _ = services.AddSingleton<IStartupService, NoOpStartupService>();
            _ = services.AddSingleton<ITrayIconService, TrayIconService>();
        }

        _ = services.AddSingleton<IDeviceCatalog, DeviceCatalog>();
        _ = services.AddSingleton<IBatteryPollingService, BatteryPollingService>();
        _ = services.AddSingleton<ISingleInstanceService, AppInstanceService>();
        _ = services.AddSingleton<IUpdateChecker>(_ =>
        {
            if (e2e)
            {
                var assembly = Assembly.LoadFrom(Path.GetFullPath(E2ETestHost.FacadeAssembly!));
                var type = assembly.GetType(
                    "BluetoothMonitor.E2E.TestDoubles.FakeUpdateChecker",
                    true
                )!;
                return new ExternalUpdateChecker(Activator.CreateInstance(type)!);
            }

            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BluetoothMonitor/1.0");
            return new GitHubUpdateChecker(client);
        });

        _ = services.AddSingleton<MainViewModel>();
        _ = services.AddTransient<DevicesViewModel>();
        _ = services.AddTransient<NotificationsViewModel>();
        _ = services.AddTransient<GeneralViewModel>();
        _ = services.AddTransient<AboutViewModel>();

        _ = services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    private static IBluetoothFacade LoadE2EFacade()
    {
        var assemblyPath = E2ETestHost.FacadeAssembly;
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            throw new InvalidOperationException(
                $"{E2ETestHost.EnvFacadeAssembly} must point to the E2E facade assembly."
            );
        }

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException(
                "The E2E Bluetooth facade assembly was not found.",
                assemblyPath
            );
        }

        var assembly = Assembly.LoadFrom(Path.GetFullPath(assemblyPath));
        var facadeType = assembly.GetType(E2ETestHost.FacadeType, throwOnError: true)!;
        return new ExternalBluetoothFacade(Activator.CreateInstance(facadeType)!);
    }

    private void OnNotificationInvoked(
        AppNotificationManager sender,
        AppNotificationActivatedEventArgs args
    )
    {
        if (args.Arguments.TryGetValue("action", out var action) && action == "open")
        {
            _ = (MainWindow?.DispatcherQueue.TryEnqueue(() => MainWindow?.ShowWindow()));
        }
    }

    private void OnUnhandledException(
        object sender,
        Microsoft.UI.Xaml.UnhandledExceptionEventArgs e
    )
    {
        Log.Logger.Fatal(e.Exception, "Unhandled UI exception");
        e.Handled = true;
    }
}
