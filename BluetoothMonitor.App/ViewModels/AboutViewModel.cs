using System;
using System.Reflection;
using BluetoothMonitor.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.System;

namespace BluetoothMonitor.App.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    private readonly IUpdateChecker _updateChecker;

    public AboutViewModel(IUpdateChecker updateChecker)
    {
        _updateChecker = updateChecker;
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

    [ObservableProperty]
    private Uri? _updateReleaseUrl;

    [RelayCommand]
    private void CheckForUpdates()
    {
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var result = await _updateChecker.CheckForUpdateAsync(Version);
            UpdateReleaseUrl = result.ReleaseUrl;
            UpdateInfoText = result.IsUpdateAvailable
                ? $"Version {result.LatestVersion} is available."
                : "You have the latest version.";
            UpdateInfoVisible = result.IsUpdateAvailable;
        }
        catch
        {
            UpdateReleaseUrl = null;
            UpdateInfoText = "Unable to check for updates. Try again later.";
            UpdateInfoVisible = true;
        }
    }

    [RelayCommand]
    private async Task OpenUpdate()
    {
        if (UpdateReleaseUrl is not null)
            await Launcher.LaunchUriAsync(UpdateReleaseUrl);
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
