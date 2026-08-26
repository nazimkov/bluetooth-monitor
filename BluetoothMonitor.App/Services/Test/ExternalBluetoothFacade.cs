using System.Collections;
using System.Reflection;
using BluetoothMonitor.App.Models;

namespace BluetoothMonitor.App.Services.Test;

// Adapts a facade from the E2E assembly without a compile-time App reference.
internal sealed class ExternalBluetoothFacade : IBluetoothFacade
{
    private readonly object _inner;
    private readonly Type _type;

    public ExternalBluetoothFacade(object inner)
    {
        _inner = inner;
        _type = inner.GetType();
    }

    public async Task<IReadOnlyList<BluetoothDeviceInfo>> ListDevicesAsync()
    {
        var value = await InvokeAsync(nameof(ListDevicesAsync));
        var devices = new List<BluetoothDeviceInfo>();
        foreach (var item in (IEnumerable?)value ?? Array.Empty<object>())
        {
            devices.Add(
                new BluetoothDeviceInfo(
                    StringProperty(item, "Id"),
                    StringProperty(item, "Name"),
                    Enum.TryParse<DeviceKind>(StringProperty(item, "Kind"), true, out var kind)
                        ? kind
                        : DeviceKind.Unknown,
                    NullableStringProperty(item, "MacAddress"),
                    (bool?)Property(item, "IsConnected") ?? true
                )
            );
        }
        return devices;
    }

    public async Task<string?> FindDeviceIdAsync(string deviceName) =>
        (string?)await InvokeAsync(nameof(FindDeviceIdAsync), deviceName);

    public async Task<byte?> GetBatteryLevelAsync(string deviceId) =>
        (byte?)await InvokeAsync(nameof(GetBatteryLevelAsync), deviceId);

    public void InvalidateCache() => InvokeAsync(nameof(InvalidateCache)).GetAwaiter().GetResult();

    private async Task<object?> InvokeAsync(string name, params object?[] args)
    {
        var method =
            _type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MissingMethodException(_type.FullName, name);
        var result = method.Invoke(_inner, args);
        if (result is not Task task)
            return result;
        await task;
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }

    private static object? Property(object? value, string name) =>
        value?.GetType().GetProperty(name)?.GetValue(value);

    private static string StringProperty(object? value, string name) =>
        Property(value, name)?.ToString() ?? "";

    private static string? NullableStringProperty(object? value, string name) =>
        Property(value, name)?.ToString();
}
