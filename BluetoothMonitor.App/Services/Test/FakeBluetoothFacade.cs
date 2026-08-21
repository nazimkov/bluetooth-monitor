using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BluetoothMonitor.App.Models;

namespace BluetoothMonitor.App.Services.Test;

public sealed class FakeBluetoothFacade : IBluetoothFacade
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly List<SeedDevice> _devices;

    public FakeBluetoothFacade()
    {
        _devices = LoadSeed(E2ETestHost.DevicesJson);
    }

    public Task<IReadOnlyList<BluetoothDeviceInfo>> ListDevicesAsync() =>
        Task.FromResult<IReadOnlyList<BluetoothDeviceInfo>>(
            _devices.Select(d => d.ToInfo()).ToList());

    public Task<string?> FindDeviceIdAsync(string deviceName)
    {
        var match = _devices.FirstOrDefault(d =>
            string.Equals(d.Name, deviceName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match?.Id);
    }

    public Task<byte?> GetBatteryLevelAsync(string deviceId)
    {
        var match = _devices.FirstOrDefault(d =>
            string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match?.BatteryLevel);
    }

    public void InvalidateCache() { }

    private static List<SeedDevice> LoadSeed(string? json)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<SeedDevice>>(json, JsonOpts);
                if (parsed is { Count: > 0 })
                {
                    return parsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"E2E device seed parse failed: {ex.Message}");
            }
        }

        return
        [
            new SeedDevice
            {
                Id = "e2e-headset",
                Name = "E2E Headset",
                Kind = DeviceKind.OverEar,
                MacAddress = "AA:BB:CC:DD:EE:01",
                IsConnected = true,
                BatteryLevel = 72
            },
            new SeedDevice
            {
                Id = "e2e-earbuds",
                Name = "E2E Earbuds",
                Kind = DeviceKind.Earbuds,
                MacAddress = "AA:BB:CC:DD:EE:02",
                IsConnected = true,
                BatteryLevel = 18
            }
        ];
    }

    private sealed class SeedDevice
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DeviceKind Kind { get; set; }
        public string? MacAddress { get; set; }
        public bool IsConnected { get; set; } = true;
        public byte BatteryLevel { get; set; }

        public BluetoothDeviceInfo ToInfo() =>
            new(Id, Name, Kind, MacAddress, IsConnected);
    }
}
