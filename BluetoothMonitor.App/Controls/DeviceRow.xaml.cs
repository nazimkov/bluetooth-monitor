using BluetoothMonitor.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BluetoothMonitor.App.Controls;

public sealed partial class DeviceRow : UserControl
{
    public DeviceRow() => InitializeComponent();

    public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register(
        nameof(Device), typeof(DeviceItemViewModel), typeof(DeviceRow), new PropertyMetadata(null));

    public DeviceItemViewModel? Device
    {
        get => (DeviceItemViewModel?)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }
}
