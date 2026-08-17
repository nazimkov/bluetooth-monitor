using System;
using Microsoft.UI.Xaml.Data;

namespace BluetoothMonitor.App.Converters;

public sealed class NullableByteToByteConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is byte b ? b : (byte)0;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
