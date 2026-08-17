using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BluetoothMonitor.App.Converters;

public sealed class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isNull = value is null || (value is string s && string.IsNullOrEmpty(s));
        var visible = Invert ? isNull : !isNull;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
