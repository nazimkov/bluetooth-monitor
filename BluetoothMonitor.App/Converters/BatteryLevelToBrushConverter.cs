using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace BluetoothMonitor.App.Converters;

public sealed class BatteryLevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        int level = value switch
        {
            byte b => b,
            int i => i,
            double d => (int)d,
            _ => 100
        };

        var app = (App)Microsoft.UI.Xaml.Application.Current;
        var resources = app.Resources;
        if (level <= 10 && resources.TryGetValue("BattcheckDangerBrush", out var danger)) return danger;
        if (level <= 20 && resources.TryGetValue("BattcheckWarnBrush", out var warn)) return warn;
        if (resources.TryGetValue("BattcheckOkBrush", out var ok)) return ok;
        return new SolidColorBrush(Microsoft.UI.Colors.Green);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
