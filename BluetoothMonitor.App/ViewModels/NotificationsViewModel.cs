using BluetoothMonitor.App.Models;
using BluetoothMonitor.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace BluetoothMonitor.App.ViewModels;

public partial class NotificationsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILogger<NotificationsViewModel> _logger;
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

    public NotificationsViewModel(ISettingsService settings, ILogger<NotificationsViewModel> logger)
    {
        _settings = settings;
        _logger = logger;
        _threshold = settings.Current.LowBatteryMaximum;
        _criticalMaximum = settings.Current.CriticalBatteryMaximum;
        _lowMaximum = settings.Current.LowBatteryMaximum;
        _notificationStyle = settings.Current.NotificationStyle;
        _alertSound = settings.Current.AlertSound;
        _criticalAlertUnder5 = settings.Current.CriticalAlertUnder5;
        _silenceDuringDnd = settings.Current.SilenceDuringDnd;
    }

    [ObservableProperty]
    private int _threshold;

    [ObservableProperty]
    private int _criticalMaximum;

    [ObservableProperty]
    private int _lowMaximum;

    [ObservableProperty]
    private string? _rangeValidationMessage;

    public int CriticalMaximumMinimum => 0;
    public int CriticalMaximumMaximum => LowMaximum - 1;
    public int LowMaximumMinimum => CriticalMaximum + 1;
    public int LowMaximumMaximum => 99;
    public string CriticalRange => BatteryRangePolicy.Critical(_settings.Current).ToString();
    public string LowRange => BatteryRangePolicy.Low(_settings.Current).ToString();
    public string NormalRange => BatteryRangePolicy.Normal(_settings.Current).ToString();
    public bool HasRangeValidationMessage => !string.IsNullOrEmpty(RangeValidationMessage);

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

    partial void OnThresholdChanged(int value) => SetLowMaximum(value);

    partial void OnCriticalMaximumChanged(int value)
    {
        if (!BatteryRangePolicy.IsCriticalMaximumValid(value, LowMaximum))
        {
            RangeValidationMessage = AppResources.Get("TrayRanges.InvalidCritical");
            OnPropertyChanged(nameof(HasRangeValidationMessage));
            _criticalMaximum = _settings.Current.CriticalBatteryMaximum;
            OnPropertyChanged(nameof(CriticalMaximum));
            return;
        }
        RangeValidationMessage = null;
        OnPropertyChanged(nameof(HasRangeValidationMessage));
        _settings.Update(s => s.CriticalBatteryMaximum = value);
        NotifyRanges();
    }

    partial void OnLowMaximumChanged(int value) => SetLowMaximum(value);

    private void SetLowMaximum(int value)
    {
        if (!BatteryRangePolicy.IsLowMaximumValid(value, CriticalMaximum))
        {
            RangeValidationMessage = AppResources.Get("TrayRanges.InvalidLow");
            OnPropertyChanged(nameof(HasRangeValidationMessage));
            _lowMaximum = _settings.Current.LowBatteryMaximum;
            OnPropertyChanged(nameof(LowMaximum));
            return;
        }
        RangeValidationMessage = null;
        OnPropertyChanged(nameof(HasRangeValidationMessage));
        _settings.Update(s =>
        {
            s.LowBatteryMaximum = value;
            s.LowBatteryThreshold = value;
        });
        if (_threshold != value)
        {
            _threshold = value;
            OnPropertyChanged(nameof(Threshold));
        }
        if (_lowMaximum != value)
        {
            _lowMaximum = value;
            OnPropertyChanged(nameof(LowMaximum));
        }
        NotifyRanges();
    }

    private void NotifyRanges()
    {
        OnPropertyChanged(nameof(CriticalMaximumMaximum));
        OnPropertyChanged(nameof(LowMaximumMinimum));
        OnPropertyChanged(nameof(CriticalRange));
        OnPropertyChanged(nameof(LowRange));
        OnPropertyChanged(nameof(NormalRange));
    }

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
            _logger.LogWarning(ex, "Unable to preview alert sound");
        }
    }
}
