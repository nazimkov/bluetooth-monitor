using BluetoothMonitor.App.Models;
using BluetoothMonitor.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace BluetoothMonitor.App.ViewModels;

public partial class NotificationsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly MediaPlayer _previewPlayer = new();

    private static readonly IReadOnlyDictionary<string, string> PreviewSoundUris = new Dictionary<
        string,
        string
    >(StringComparer.OrdinalIgnoreCase)
    {
        ["Gentle"] = "ms-winsoundevent:Notification.Default",
        ["Ping"] = "ms-winsoundevent:Notification.IM",
        ["Alert"] = "ms-winsoundevent:Notification.Mail",
    };

    public NotificationsViewModel(ISettingsService settings)
    {
        _settings = settings;
        _threshold = settings.Current.LowBatteryThreshold;
        _notificationStyle = settings.Current.NotificationStyle;
        _alertSound = settings.Current.AlertSound;
        _criticalAlertUnder5 = settings.Current.CriticalAlertUnder5;
        _silenceDuringDnd = settings.Current.SilenceDuringDnd;
    }

    [ObservableProperty]
    private int _threshold;

    [ObservableProperty]
    private NotificationStyle _notificationStyle;

    [ObservableProperty]
    private string _alertSound = "Gentle";

    [ObservableProperty]
    private bool _criticalAlertUnder5;

    [ObservableProperty]
    private bool _silenceDuringDnd;

    public int NotificationStyleIndex
    {
        get => (int)NotificationStyle;
        set
        {
            if (System.Enum.IsDefined(typeof(NotificationStyle), value))
                NotificationStyle = (NotificationStyle)value;
        }
    }

    partial void OnThresholdChanged(int value) =>
        _settings.Update(s => s.LowBatteryThreshold = value);

    partial void OnNotificationStyleChanged(NotificationStyle value)
    {
        _settings.Update(s => s.NotificationStyle = value);
        OnPropertyChanged(nameof(NotificationStyleIndex));
    }

    partial void OnAlertSoundChanged(string value) => _settings.Update(s => s.AlertSound = value);

    partial void OnCriticalAlertUnder5Changed(bool value) =>
        _settings.Update(s => s.CriticalAlertUnder5 = value);

    partial void OnSilenceDuringDndChanged(bool value) =>
        _settings.Update(s => s.SilenceDuringDnd = value);

    [RelayCommand]
    private void PreviewSound()
    {
        if (!PreviewSoundUris.TryGetValue(AlertSound, out var soundUri))
            return;

        try
        {
            _previewPlayer.Source = MediaSource.CreateFromUri(new Uri(soundUri));
            _previewPlayer.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to preview alert sound: {ex}");
        }
    }
}
