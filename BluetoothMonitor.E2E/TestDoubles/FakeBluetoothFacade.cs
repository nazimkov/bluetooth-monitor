using System.Text.Json;
using System.Text.Json.Serialization;

namespace BluetoothMonitor.E2E.TestDoubles;

public sealed class FakeBluetoothFacade
{
    public const string HeadsetId = "e2e-headset";
    public const string HeadsetName = "E2E Headset";
    public const string EarbudsId = "e2e-earbuds";
    public const string EarbudsName = "E2E Earbuds";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly List<SeedDevice> _devices = LoadSeed();

    public Task<IReadOnlyList<FakeDeviceInfo>> ListDevicesAsync() =>
        Task.FromResult<IReadOnlyList<FakeDeviceInfo>>(_devices.Select(d => d.ToInfo()).ToList());

    public Task<string?> FindDeviceIdAsync(string deviceName) =>
        Task.FromResult(
            _devices
                .FirstOrDefault(d =>
                    string.Equals(d.Name, deviceName, StringComparison.OrdinalIgnoreCase)
                )
                ?.Id
        );

    public Task<byte?> GetBatteryLevelAsync(string deviceId) =>
        Task.FromResult(
            _devices
                .FirstOrDefault(d =>
                    string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase)
                )
                ?.GetBatteryLevel()
        );

    public void InvalidateCache() { }

    private static List<SeedDevice> LoadSeed()
    {
        var json = Environment.GetEnvironmentVariable("BATTCHECK_E2E_DEVICES_JSON");
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<SeedDevice>>(json, JsonOpts);
                if (parsed is { Count: > 0 })
                    return parsed;
            }
            catch (JsonException) { }
        }

        return
        [
            new()
            {
                Id = HeadsetId,
                Name = HeadsetName,
                Kind = "OverEar",
                MacAddress = "AA:BB:CC:DD:EE:01",
                BatteryLevel = 72,
            },
            new()
            {
                Id = EarbudsId,
                Name = EarbudsName,
                Kind = "Earbuds",
                MacAddress = "AA:BB:CC:DD:EE:02",
                BatteryLevel = 18,
            },
        ];
    }

    private sealed class SeedDevice
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "Unknown";
        public string? MacAddress { get; set; }
        public bool IsConnected { get; set; } = true;
        public byte BatteryLevel { get; set; }

        public byte GetBatteryLevel()
        {
            var path = Environment.GetEnvironmentVariable("BATTCHECK_E2E_BATTERY_LEVEL_FILE");
            if (
                !string.IsNullOrWhiteSpace(path)
                && File.Exists(path)
                && byte.TryParse(File.ReadAllText(path), out var level)
            )
            {
                return level;
            }

            return BatteryLevel;
        }

        public FakeDeviceInfo ToInfo() => new(Id, Name, Kind, MacAddress, IsConnected);
    }

    public sealed record FakeDeviceInfo(
        string Id,
        string Name,
        string Kind,
        string? MacAddress,
        bool IsConnected
    );
}
