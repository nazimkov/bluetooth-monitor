using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.System;

namespace BluetoothMonitor.App.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    public AboutViewModel()
    {
        Version = GetVersion();
        Platform = Environment.OSVersion.VersionString;
    }

    [ObservableProperty]
    private string _version = "1.0.0";

    [ObservableProperty]
    private string _platform = "Windows 11 22H2 or later";

    [ObservableProperty]
    private bool _updateInfoVisible;

    [ObservableProperty]
    private string _updateInfoText = string.Empty;

    [RelayCommand]
    private void CheckForUpdates()
    {
        UpdateInfoText = "No update server configured.";
        UpdateInfoVisible = true;
    }

    [RelayCommand]
    private async Task OpenPrivacyPolicy()
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/"));
    }

    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        assemblyVersion = assemblyVersion?.Split('+')[0]; // Remove build metadata if present

        return assemblyVersion
            ?? throw new ApplicationException("Unable to determine assembly version.");
    }
}
