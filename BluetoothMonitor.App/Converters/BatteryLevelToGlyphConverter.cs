using System;
using Microsoft.UI.Xaml.Data;

namespace BluetoothMonitor.App.Converters;

public sealed class BatteryLevelToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        int level = value switch
        {
            byte b => b,
            int i => i,
            double d => (int)d,
            _ => 100,
        };
        var step = Math.Clamp(level / 10, 0, 10);
        return ((char)(0xEBA0 + step)).ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
