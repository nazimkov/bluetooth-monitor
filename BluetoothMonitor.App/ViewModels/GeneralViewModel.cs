using System.Threading.Tasks;
using BluetoothMonitor.App.Models;
using BluetoothMonitor.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BluetoothMonitor.App.ViewModels;

public partial class GeneralViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IStartupService _startup;

    public GeneralViewModel(ISettingsService settings, IStartupService startup)
    {
        _settings = settings;
        _startup = startup;

        _startAtSignIn = settings.Current.StartAtSignIn;
        _keepRunningInBackground = settings.Current.KeepRunningInBackground;
        _refreshIntervalSeconds = settings.Current.RefreshIntervalSeconds;
        _theme = settings.Current.Theme;
    }

    [ObservableProperty]
    private bool _startAtSignIn;

    [ObservableProperty]
    private bool _keepRunningInBackground;

    [ObservableProperty]
    private int _refreshIntervalSeconds;

    [ObservableProperty]
    private AppTheme _theme;

    public int RefreshIndex
    {
        get =>
            RefreshIntervalSeconds switch
            {
                15 => 0,
                30 => 1,
                60 => 2,
                300 => 3,
                600 => 4,
                _ => 2,
            };
        set =>
            RefreshIntervalSeconds = value switch
            {
                0 => 15,
                1 => 30,
                2 => 60,
                3 => 300,
                4 => 600,
                _ => 60,
            };
    }

    public int ThemeIndex
    {
        get => (int)Theme;
        set
        {
            if (System.Enum.IsDefined(typeof(AppTheme), value))
                Theme = (AppTheme)value;
        }
    }

    partial void OnStartAtSignInChanged(bool value)
    {
        _settings.Update(s => s.StartAtSignIn = value);
        _ = Task.Run(() => _startup.SetEnabledAsync(value));
    }

    partial void OnKeepRunningInBackgroundChanged(bool value) =>
        _settings.Update(s => s.KeepRunningInBackground = value);

    partial void OnRefreshIntervalSecondsChanged(int value)
    {
        _settings.Update(s => s.RefreshIntervalSeconds = value);
        OnPropertyChanged(nameof(RefreshIndex));
    }

    partial void OnThemeChanged(AppTheme value)
    {
        _settings.Update(s => s.Theme = value);
        OnPropertyChanged(nameof(ThemeIndex));
    }
}
