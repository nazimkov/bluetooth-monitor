using System;
using BluetoothMonitor.App.Services;
using BluetoothMonitor.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using WinRT;

namespace BluetoothMonitor.App;

public partial class App : Application
{
    public static App Current => (App)Application.Current;
    public IServiceProvider Services { get; private set; } = null!;
    public MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Services = ConfigureServices();

        var settings = Services.GetRequiredService<ISettingsService>();
        await settings.LoadAsync();

        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
        AppNotificationManager.Default.Register();

        var instance = Services.GetRequiredService<ISingleInstanceService>();
        _ = instance.RedirectIfNotPrimary();

        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Activate();

        var polling = Services.GetRequiredService<IBatteryPollingService>();
        _ = polling.StartAsync(default);

        var tray = Services.GetRequiredService<ITrayIconService>();
        tray.Initialize();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<IBluetoothFacade, BluetoothFacade>();
        services.AddSingleton<IDeviceCatalog, DeviceCatalog>();
        services.AddSingleton<IBatteryPollingService, BatteryPollingService>();
        services.AddSingleton<INotificationService, AppNotificationService>();
        services.AddSingleton<IStartupService, StartupTaskService>();
        services.AddSingleton<ISingleInstanceService, AppInstanceService>();
        services.AddSingleton<ITrayIconService, TrayIconService>();

        services.AddSingleton<MainViewModel>();
        services.AddTransient<DevicesViewModel>();
        services.AddTransient<NotificationsViewModel>();
        services.AddTransient<GeneralViewModel>();
        services.AddTransient<AboutViewModel>();

        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        if (args.Arguments.TryGetValue("action", out var action) && action == "open")
        {
            MainWindow?.DispatcherQueue.TryEnqueue(() => MainWindow?.ShowWindow());
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Unhandled: {e.Exception}");
        e.Handled = true;
    }
}
