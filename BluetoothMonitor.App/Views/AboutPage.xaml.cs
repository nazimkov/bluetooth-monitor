using BluetoothMonitor.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace BluetoothMonitor.App.Views;

public sealed partial class AboutPage : Page
{
    public AboutViewModel ViewModel { get; }

    public AboutPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<AboutViewModel>();
        InitializeComponent();
    }
}
