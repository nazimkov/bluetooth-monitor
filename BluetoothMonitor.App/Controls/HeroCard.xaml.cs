using System.Windows.Input;
using BluetoothMonitor.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BluetoothMonitor.App.Controls;

public sealed partial class HeroCard : UserControl
{
    public HeroCard() => InitializeComponent();

    public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register(
        nameof(Device), typeof(DeviceItemViewModel), typeof(HeroCard), new PropertyMetadata(null));

    public DeviceItemViewModel? Device
    {
        get => (DeviceItemViewModel?)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    public static readonly DependencyProperty RefreshCommandProperty = DependencyProperty.Register(
        nameof(RefreshCommand), typeof(ICommand), typeof(HeroCard), new PropertyMetadata(null));

    public ICommand? RefreshCommand
    {
        get => (ICommand?)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }
}
