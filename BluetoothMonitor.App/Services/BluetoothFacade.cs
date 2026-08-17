using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BluetoothMonitor.App.Models;
using BluetoothMonitor.Core;
using BluetoothMonitor.Core.Devices;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace BluetoothMonitor.App.Services;

public sealed class BluetoothFacade : IBluetoothFacade
{
    private readonly IBluetoothDevices _classic = new BluetoothClassicDevices();
    private readonly IBluetoothDevices _le = new BluetoothLEDevices();
    private readonly ConcurrentDictionary<string, bool> _isLeCache = new();

    public async Task<IReadOnlyList<BluetoothDeviceInfo>> ListDevicesAsync()
    {
        _isLeCache.Clear();

        var classic = await _classic.ListDevicesAsync();
        var le = await _le.ListDevicesAsync();

        var merged = classic
            .Concat(le)
            .GroupBy(d => d.Id)
            .Select(g => g.First())
            .ToList();

        var result = new List<BluetoothDeviceInfo>(merged.Count);
        foreach (var d in merged)
        {
            result.Add(Project(d));
        }
        return result;
    }

    public async Task<string?> FindDeviceIdAsync(string deviceName)
    {
        var leId = await _le.FindDeviceIdAsync(deviceName);
        if (!string.IsNullOrWhiteSpace(leId)) return leId;
        return await _classic.FindDeviceIdAsync(deviceName);
    }

    public async Task<byte?> GetBatteryLevelAsync(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return null;

        var service = await ResolveServiceAsync(deviceId);
        try
        {
            var result = await service.CheckBatteryLevelAsync(deviceId);
            return result.BatteryLevel;
        }
        catch (BluetoothException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void InvalidateCache() => _isLeCache.Clear();

    private async Task<IBluetoothDevices> ResolveServiceAsync(string deviceId)
    {
        if (_isLeCache.TryGetValue(deviceId, out var cached))
        {
            return cached ? _le : _classic;
        }
        var isLe = await IsLeAsync(deviceId);
        _isLeCache[deviceId] = isLe;
        return isLe ? _le : _classic;
    }

    private static async Task<bool> IsLeAsync(string deviceId)
    {
        try
        {
            using var device = await BluetoothLEDevice.FromIdAsync(deviceId);
            return device is not null;
        }
        catch
        {
            return false;
        }
    }

    private static BluetoothDeviceInfo Project(DeviceInformation info)
    {
        var kind = GuessKind(info);
        var mac = ExtractMac(info);
        var connected = IsConnected(info);
        return new BluetoothDeviceInfo(info.Id, info.Name ?? "Unknown", kind, mac, connected);
    }

    private static DeviceKind GuessKind(DeviceInformation info)
    {
        if (info.Properties.TryGetValue("System.Devices.Aep.Bluetooth.Cod.Major", out var majorObj)
            && majorObj is uint major
            && major == 0x04)
        {
            if (info.Properties.TryGetValue("System.Devices.Aep.Bluetooth.Cod.Minor", out var minorObj)
                && minorObj is uint minor)
            {
                return minor switch
                {
                    0x01 => DeviceKind.Handsfree,
                    0x02 => DeviceKind.Handsfree,
                    0x04 => DeviceKind.Speaker,
                    0x05 => DeviceKind.OverEar,
                    0x06 => DeviceKind.Earbuds,
                    _ => DeviceKind.Unknown
                };
            }
        }

        var name = (info.Name ?? string.Empty).ToLowerInvariant();
        if (name.Contains("buds") || name.Contains("earbud") || name.Contains("airpod")) return DeviceKind.Earbuds;
        if (name.Contains("speaker") || name.Contains("boom")) return DeviceKind.Speaker;
        if (name.Contains("head") || name.Contains("phone")) return DeviceKind.OverEar;
        return DeviceKind.Unknown;
    }

    private static string? ExtractMac(DeviceInformation info)
    {
        if (info.Properties.TryGetValue("System.Devices.Aep.DeviceAddress", out var raw) && raw is string s)
        {
            return s.Length == 12 ? FormatMac(s) : s;
        }
        return null;
    }

    private static string FormatMac(string hex)
    {
        if (hex.Length != 12) return hex;
        return $"{hex[..2]}:{hex[2..4]}:{hex[4..6]}:{hex[6..8]}:{hex[8..10]}:{hex[10..12]}".ToUpperInvariant();
    }

    private static bool IsConnected(DeviceInformation info)
    {
        if (info.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var value) && value is bool b)
        {
            return b;
        }
        return false;
    }
}
