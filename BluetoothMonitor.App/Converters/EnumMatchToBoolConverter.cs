using System;
using Microsoft.UI.Xaml.Data;

namespace BluetoothMonitor.App.Converters;

public sealed class EnumMatchToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null || parameter is null) return false;
        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b && b && parameter is not null && targetType.IsEnum)
        {
            return Enum.Parse(targetType, parameter.ToString()!, ignoreCase: true);
        }
        return null!;
    }
}
