using BluetoothMonitor.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace BluetoothMonitor.App.Views;

public sealed partial class NotificationsPage : Page
{
    public NotificationsViewModel ViewModel { get; }

    public NotificationsPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<NotificationsViewModel>();
        InitializeComponent();
    }
}
