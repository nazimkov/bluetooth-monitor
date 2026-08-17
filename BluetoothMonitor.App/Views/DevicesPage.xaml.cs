using BluetoothMonitor.App.Controls;
using BluetoothMonitor.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace BluetoothMonitor.App.Views;

public sealed partial class DevicesPage : Page
{
    public DevicesViewModel ViewModel { get; }

    public DevicesPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<DevicesViewModel>();
        InitializeComponent();
        Loaded += DevicesPage_Loaded;
    }

    private void DevicesPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.PollNowCommand.CanExecute(null))
        {
            ViewModel.PollNowCommand.Execute(null);
        }
    }

    private void DeviceRow_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is DeviceRow row && row.Device is DeviceItemViewModel vm)
        {
            ViewModel.SelectDeviceCommand.Execute(vm);
        }
    }
}
