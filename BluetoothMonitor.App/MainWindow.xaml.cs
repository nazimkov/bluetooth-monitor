using System;
using System.Collections.Generic;
using System.Linq;
using BluetoothMonitor.App.Services;
using BluetoothMonitor.App.ViewModels;
using BluetoothMonitor.App.Views;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;

namespace BluetoothMonitor.App;

public sealed partial class MainWindow : Window
{
    private sealed record SettingSearchResult(string Text, Type Page)
    {
        public override string ToString() => Text;
    };

    private readonly ISettingsService _settings;
    private bool _reallyClose;
    private int _exitRequested;
    private readonly ILogger<MainWindow> _logger;

    public MainViewModel ViewModel { get; }

    public MainWindow(
        MainViewModel viewModel,
        ISettingsService settings,
        ILogger<MainWindow> logger
    )
    {
        ViewModel = viewModel;
        _settings = settings;
        _logger = logger;

        InitializeComponent();

        Title = "Bluetooth Monitor";
        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        var hwnd = WindowNative.GetWindowHandle(this);
        var id = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(id);
        appWindow.Resize(new SizeInt32(920, 640));
        appWindow.SetIcon("Assets\\app.ico");

        ApplyTheme();
        _settings.Changed += OnSettingsChanged;

        ContentFrame.Navigate(typeof(DevicesPage));

        NavSearch.ItemsSource = CreateSettingSearchIndex();

        Closed += OnClosed;
    }

    private static IReadOnlyList<SettingSearchResult> CreateSettingSearchIndex() =>
        new[]
        {
            new SettingSearchResult(
                AppResources.Get("StartAtSignInCard.Header"),
                typeof(GeneralPage)
            ),
            new SettingSearchResult(
                AppResources.Get("KeepRunningCard.Header"),
                typeof(GeneralPage)
            ),
            new SettingSearchResult(
                AppResources.Get("RefreshIntervalCard.Header"),
                typeof(GeneralPage)
            ),
            new SettingSearchResult(AppResources.Get("ThemeCard.Header"), typeof(GeneralPage)),
            new SettingSearchResult(
                AppResources.Get("LowBatteryThresholdCard.Header"),
                typeof(NotificationsPage)
            ),
            new SettingSearchResult(
                AppResources.Get("NotificationStyleCard.Header"),
                typeof(NotificationsPage)
            ),
            new SettingSearchResult(
                AppResources.Get("AlertSoundCard.Header"),
                typeof(NotificationsPage)
            ),
            new SettingSearchResult(
                AppResources.Get("CriticalAlertCard.Header"),
                typeof(NotificationsPage)
            ),
            new SettingSearchResult(
                AppResources.Get("SilenceDndCard.Header"),
                typeof(NotificationsPage)
            ),
        };

    private void NavSearch_TextChanged(
        AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args
    )
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        var query = sender.Text.Trim();
        sender.ItemsSource = string.IsNullOrEmpty(query)
            ? CreateSettingSearchIndex()
            :
            [
                .. CreateSettingSearchIndex()
                    .Where(result =>
                        result.Text.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                    ),
            ];
    }

    private void NavSearch_SuggestionChosen(
        AutoSuggestBox sender,
        AutoSuggestBoxSuggestionChosenEventArgs args
    )
    {
        var result =
            args.SelectedItem as SettingSearchResult
            ?? CreateSettingSearchIndex()
                .FirstOrDefault(item =>
                    string.Equals(
                        item.Text,
                        sender.Text.Trim(),
                        StringComparison.CurrentCultureIgnoreCase
                    )
                );

        if (result is not null)
            NavigateToSetting(result);
    }

    private void NavSearch_QuerySubmitted(
        AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args
    )
    {
        var result = args.ChosenSuggestion as SettingSearchResult;

        if (result is not null)
            NavigateToSetting(result);
    }

    private void NavigateToSetting(SettingSearchResult result)
    {
        if (ContentFrame.CurrentSourcePageType != result.Page)
            ContentFrame.Navigate(result.Page);

        RootNav.SelectedItem = result.Page == typeof(GeneralPage) ? NavGeneral : NavNotifications;
    }

    private void OnSettingsChanged(object? sender, EventArgs e) => ApplyTheme();

    private void ApplyTheme()
    {
        if (RootGrid is null)
            return;
        RootGrid.RequestedTheme = _settings.Current.Theme switch
        {
            Models.AppTheme.Light => ElementTheme.Light,
            Models.AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
    }

    private void RootNav_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args
    )
    {
        if (args.SelectedItem is not NavigationViewItem item)
            return;
        Type? page = (item.Tag as string) switch
        {
            "devices" => typeof(DevicesPage),
            "notifications" => typeof(NotificationsPage),
            "general" => typeof(GeneralPage),
            "about" => typeof(AboutPage),
            _ => null,
        };
        if (page is not null && ContentFrame.CurrentSourcePageType != page)
        {
            ContentFrame.Navigate(page);
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (_reallyClose)
            return;
        if (_settings.Current.KeepRunningInBackground)
        {
            args.Handled = true;
            HideWindow();
        }
    }

    public void HideWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var id = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(id);
        appWindow.Hide();
    }

    public void ShowWindow()
    {
        if (!DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(ShowWindow);
            return;
        }

        var hwnd = WindowNative.GetWindowHandle(this);
        var id = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(id);
        appWindow.Show();
        Activate();
    }

    public void ExitApplication()
    {
        if (Interlocked.Exchange(ref _exitRequested, 1) != 0)
            return;

        _reallyClose = true;
        _logger.LogInformation("Application shutdown requested");

        void Exit()
        {
            _logger.LogInformation("Application shutting down");
            App.Current.Exit();
            var tray =
                App.Current.Services.GetService(typeof(ITrayIconService)) as ITrayIconService;
            tray?.Dispose();
            AppLogging.CloseAndFlush();
        }

        if (!App.UiDispatcherQueue.TryEnqueue(Exit))
        {
            Volatile.Write(ref _exitRequested, 0);
        }
    }
}
