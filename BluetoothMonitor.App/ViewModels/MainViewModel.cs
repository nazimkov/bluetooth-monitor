using BluetoothMonitor.App.Models;
using BluetoothMonitor.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BluetoothMonitor.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel(ISettingsService settings)
    {
        _theme = settings.Current.Theme;
        settings.Changed += (_, _) => Theme = settings.Current.Theme;
    }

    [ObservableProperty]
    private AppTheme _theme;
}
