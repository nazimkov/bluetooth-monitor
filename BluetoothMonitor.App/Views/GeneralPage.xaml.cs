using BluetoothMonitor.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace BluetoothMonitor.App.Views;

public sealed partial class GeneralPage : Page
{
    public GeneralViewModel ViewModel { get; }

    public GeneralPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<GeneralViewModel>();
        InitializeComponent();
    }
}
