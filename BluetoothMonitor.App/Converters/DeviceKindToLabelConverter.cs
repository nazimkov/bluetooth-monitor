using System;
using BluetoothMonitor.App.Models;
using Microsoft.UI.Xaml.Data;

namespace BluetoothMonitor.App.Converters;

public sealed class DeviceKindToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var kind = value is DeviceKind dk ? dk : DeviceKind.Unknown;
        return kind switch
        {
            DeviceKind.Earbuds => "True wireless earbuds",
            DeviceKind.OverEar => "Over-ear headphones",
            DeviceKind.Speaker => "Bluetooth speaker",
            DeviceKind.Handsfree => "Handsfree headset",
            _ => "Audio device"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
