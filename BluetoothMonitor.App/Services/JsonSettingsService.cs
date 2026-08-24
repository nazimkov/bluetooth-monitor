using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BluetoothMonitor.App.Models;

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
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private SettingsModel _current = new();
    private CancellationTokenSource? _debounceCts;

    public JsonSettingsService()
        : this(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Battcheck"
            )
        ) { }

    public JsonSettingsService(string directory)
    {
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
                await SaveAsync();
                return;
            }
            await using var stream = File.OpenRead(_filePath);
            var loaded = await JsonSerializer.DeserializeAsync<SettingsModel>(stream, JsonOpts);
            if (loaded is not null)
            {
                _current = loaded;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Settings load failed: {ex.Message}");
        }
    }

    public async Task SaveAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, _current, JsonOpts);
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
