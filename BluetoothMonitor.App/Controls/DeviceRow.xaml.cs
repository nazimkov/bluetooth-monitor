using BluetoothMonitor.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace BluetoothMonitor.App.Controls;

public sealed partial class DeviceRow : UserControl
{
    public DeviceRow() => InitializeComponent();

    public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register(
        nameof(Device), typeof(DeviceItemViewModel), typeof(DeviceRow),
        new PropertyMetadata(null, OnDeviceChanged));

    public DeviceItemViewModel? Device
    {
        get => (DeviceItemViewModel?)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    private static void OnDeviceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DeviceRow row)
        {
            return;
        }

        if (e.OldValue is DeviceItemViewModel oldDevice)
        {
            oldDevice.PropertyChanged -= row.OnDevicePropertyChanged;
        }

        if (e.NewValue is DeviceItemViewModel device)
        {
            device.PropertyChanged += row.OnDevicePropertyChanged;
            row.ApplyAutomationIds(device);
        }
        else
        {
            row.ApplyAutomationIds(null);
        }
    }

    private void OnDevicePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DeviceItemViewModel.Id) or nameof(DeviceItemViewModel.Name))
        {
            ApplyAutomationIds(Device);
        }
    }

    private void ApplyAutomationIds(DeviceItemViewModel? device)
    {
        var id = device is { Id.Length: > 0 } ? $"DeviceRow_{device.Id}" : "DeviceRow_Unknown";
        AutomationProperties.SetAutomationId(this, id);
        AutomationProperties.SetAutomationId(RootBorder, id);
        if (device is { Name.Length: > 0 })
        {
            AutomationProperties.SetName(RootBorder, device.Name);
            AutomationProperties.SetAutomationId(DeviceNameText, $"DeviceName_{device.Id}");
        }
    }
}
