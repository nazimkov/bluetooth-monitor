using System;
using BluetoothMonitor.App.Services;
using BluetoothMonitor.App.ViewModels;
using BluetoothMonitor.App.Views;
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
    private readonly ISettingsService _settings;
    private bool _reallyClose;

    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel, ISettingsService settings)
    {
        ViewModel = viewModel;
        _settings = settings;

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

        Closed += OnClosed;
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
        var hwnd = WindowNative.GetWindowHandle(this);
        var id = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(id);
        appWindow.Show();
        Activate();
    }

    public void ExitApplication()
    {
        _reallyClose = true;
        Microsoft.UI.Xaml.Application.Current.Exit();
    }
}
