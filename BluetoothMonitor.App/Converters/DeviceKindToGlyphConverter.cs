using System;
using BluetoothMonitor.App.Models;
using Microsoft.UI.Xaml.Data;

namespace BluetoothMonitor.App.Converters;

public sealed class DeviceKindToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var kind = value is DeviceKind dk ? dk : DeviceKind.Unknown;
        return kind switch
        {
            DeviceKind.Speaker => "\uE7F5",
            DeviceKind.Earbuds => "\uE7F6",
            DeviceKind.OverEar => "\uE7F6",
            DeviceKind.Handsfree => "\uE7F6",
            _ => "\uE7F6"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
