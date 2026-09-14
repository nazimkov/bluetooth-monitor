using System;
using BluetoothMonitor.App.Models;
using BluetoothMonitor.App.Services;
using Microsoft.UI.Xaml.Data;

namespace BluetoothMonitor.App.Converters;

public sealed class DeviceKindToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var kind = value is DeviceKind dk ? dk : DeviceKind.Unknown;
        return kind switch
        {
            DeviceKind.Earbuds => AppResources.Get("DeviceKind.Earbuds"),
            DeviceKind.OverEar => AppResources.Get("DeviceKind.OverEar"),
            DeviceKind.Speaker => AppResources.Get("DeviceKind.Speaker"),
            DeviceKind.Handsfree => AppResources.Get("DeviceKind.Handsfree"),
            _ => AppResources.Get("DeviceKind.Unknown"),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
