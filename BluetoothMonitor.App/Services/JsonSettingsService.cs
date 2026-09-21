using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BluetoothMonitor.App.Models;
using Microsoft.Extensions.Logging;

namespace BluetoothMonitor.App.Services;

public sealed class JsonSettingsService : ISettingsService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _filePath;
    private readonly ILogger<JsonSettingsService> _logger;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private SettingsModel _current = new();
    private CancellationTokenSource? _debounceCts;

    public JsonSettingsService(ILogger<JsonSettingsService> logger)
        : this(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BluetoothMonitor"
            )
        )
    {
        _logger = logger;
    }

    public JsonSettingsService(string directory, ILogger<JsonSettingsService>? logger = null)
    {
        _logger =
            logger
            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<JsonSettingsService>.Instance;
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "settings.json");
    }

    public string FilePath => _filePath;

    public SettingsModel Current => _current;
    public event EventHandler? Changed;

    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogInformation("Settings file not found. Creating default settings");
                await SaveAsync();
                return;
            }
            await using var stream = File.OpenRead(_filePath);
            var loaded = await JsonSerializer.DeserializeAsync<SettingsModel>(stream, JsonOpts);
            if (loaded is not null)
            {
                // Older files have only lowBatteryThreshold. Preserve it as the new low maximum.
                loaded.CriticalBatteryMaximum = Math.Clamp(loaded.CriticalBatteryMaximum, 0, 98);
                var lowMaximum = loaded.LowBatteryMaximum;
                if (
                    lowMaximum == SettingsModelDefaults.DefaultLowMaximum
                    && loaded.LowBatteryThreshold != SettingsModelDefaults.DefaultLowMaximum
                )
                    lowMaximum = loaded.LowBatteryThreshold;
                loaded.LowBatteryMaximum = Math.Clamp(
                    lowMaximum,
                    loaded.CriticalBatteryMaximum + 1,
                    99
                );
                loaded.LowBatteryThreshold = loaded.LowBatteryMaximum;
                _current = loaded;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings load failed");
        }
    }

    public async Task SaveAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, _current, JsonOpts);
            _logger.LogDebug("Settings saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings save failed");
            throw;
        }
        finally
        {
            _saveLock.Release();
        }
    }

    public void Update(Action<SettingsModel> mutate)
    {
        mutate(_current);
        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSave();
    }

    private void ScheduleSave()
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                {
                    await SaveAsync();
                }
            }
            catch (TaskCanceledException) { }
        });
    }

    public void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _saveLock.Dispose();
    }
}

internal static class SettingsModelDefaults
{
    public const int DefaultLowMaximum = 20;
}
