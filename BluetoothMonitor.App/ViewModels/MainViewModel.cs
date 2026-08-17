using BluetoothMonitor.App.Models;
using BluetoothMonitor.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BluetoothMonitor.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    public MainViewModel(ISettingsService settings)
    {
        _settings = settings;
        _theme = settings.Current.Theme;
        settings.Changed += (_, _) => Theme = settings.Current.Theme;
    }

    [ObservableProperty] private AppTheme _theme;
}
