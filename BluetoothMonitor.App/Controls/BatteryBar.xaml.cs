using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BluetoothMonitor.App.Controls;

public sealed partial class BatteryBar : UserControl
{
    public BatteryBar()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateWidth();
    }

    public static readonly DependencyProperty PercentLevelProperty = DependencyProperty.Register(
        nameof(PercentLevel),
        typeof(byte),
        typeof(BatteryBar),
        new PropertyMetadata((byte)0, OnVisualChanged)
    );

    public byte PercentLevel
    {
        get => (byte)GetValue(PercentLevelProperty);
        set => SetValue(PercentLevelProperty, value);
    }

    public string PercentText => $"{PercentLevel}%";

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BatteryBar bar)
            bar.UpdateWidth();
    }

    private void UpdateWidth()
    {
        Bindings?.Update();
        if (FillBorder is null)
            return;
        var pct = Math.Clamp(PercentLevel / 100.0, 0.0, 1.0);
        FillBorder.Width = 140 * pct;
    }
}
