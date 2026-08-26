using System.Diagnostics;
using System.Text.Json;
using BluetoothMonitor.E2E.TestDoubles;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace BluetoothMonitor.E2E.Fixtures;

[CollectionDefinition("E2E")]
public sealed class E2ECollection : ICollectionFixture<AppLifecycle> { }

public sealed class AppLifecycle : IDisposable
{
    private readonly UIA3Automation _automation = new();
    private Application? _application;
    private Process? _process;
    private bool _disposed;

    public string RunId { get; } = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
    public string SettingsDirectory { get; }
    public string InstanceKey { get; }
    public string ExePath { get; }
    public Window MainWindow { get; private set; } = null!;
    public AutomationBase Automation => _automation;

    public AppLifecycle()
    {
        SettingsDirectory = Path.Combine(Path.GetTempPath(), "BluetoothMonitorE2E", RunId);
        Directory.CreateDirectory(SettingsDirectory);
        InstanceKey = $"BluetoothMonitor.App.E2E.{RunId}";
        ExePath = ResolveExePath();

        KillLeftoverProcesses();
        Launch();
    }

    public string SettingsFilePath => Path.Combine(SettingsDirectory, "settings.json");
    public string NotificationLogPath => Path.Combine(SettingsDirectory, "notifications.jsonl");
    public string FakeBatteryLevelPath => Path.Combine(SettingsDirectory, "battery-level.txt");

    public void SetFakeBatteryLevel(byte level) =>
        File.WriteAllText(FakeBatteryLevelPath, level.ToString());

    public async Task<JsonDocument> WaitForNotificationAsync(
        Func<JsonElement, bool> predicate,
        TimeSpan? timeout = null
    )
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(NotificationLogPath))
            {
                foreach (var line in await File.ReadAllLinesAsync(NotificationLogPath))
                {
                    try
                    {
                        var document = JsonDocument.Parse(line);
                        if (predicate(document.RootElement))
                            return document;
                        document.Dispose();
                    }
                    catch (JsonException) { }
                }
            }

            await Task.Delay(100);
        }

        throw new TimeoutException(
            $"Expected notification was not recorded in {NotificationLogPath}"
        );
    }

    public async Task<JsonDocument> WaitForSettingsAsync(TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(SettingsFilePath))
            {
                await using var stream = File.OpenRead(SettingsFilePath);
                return await JsonDocument.ParseAsync(stream);
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"settings.json not written to {SettingsFilePath}");
    }

    public async Task WaitForSettingsValueAsync(
        Func<JsonElement, bool> predicate,
        TimeSpan? timeout = null
    )
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    await using var stream = File.OpenRead(SettingsFilePath);
                    using var doc = await JsonDocument.ParseAsync(stream);
                    if (predicate(doc.RootElement))
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                last = ex;
            }

            await Task.Delay(150);
        }

        throw new TimeoutException(
            $"settings.json predicate not satisfied. Last error: {last?.Message}"
        );
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        // Prefer kill over Close(): WinUI/MSIX often ignores WM_CLOSE cleanly under test.
        KillLeftoverProcesses();

        try
        {
            _application?.Dispose();
        }
        catch
        {
            // ignored
        }

        try
        {
            _automation.Dispose();
        }
        catch
        {
            // ignored
        }

        try
        {
            _process?.Dispose();
        }
        catch
        {
            // ignored
        }

        try
        {
            if (Directory.Exists(SettingsDirectory))
            {
                Directory.Delete(SettingsDirectory, recursive: true);
            }
        }
        catch
        {
            // ignored
        }
    }

    private void Launch()
    {
        if (!File.Exists(ExePath))
        {
            throw new FileNotFoundException(
                $"BluetoothMonitor exe not found at '{ExePath}'. Build the App project first: "
                    + "dotnet build BluetoothMonitor.App -c Debug -p:Platform=x64",
                ExePath
            );
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = ExePath,
            WorkingDirectory = Path.GetDirectoryName(ExePath)!,
            UseShellExecute = false,
        };
        startInfo.Environment["BLUETOOTHMONITOR_E2E"] = "1";
        startInfo.Environment["BLUETOOTHMONITOR_E2E_SETTINGS_DIR"] = SettingsDirectory;
        startInfo.Environment["BLUETOOTHMONITOR_E2E_INSTANCE_KEY"] = InstanceKey;
        startInfo.Environment["BLUETOOTHMONITOR_E2E_SELECTED_DEVICE_ID"] =
            FakeBluetoothFacade.EarbudsId;
        startInfo.Environment["BLUETOOTHMONITOR_E2E_BATTERY_LEVEL_FILE"] = FakeBatteryLevelPath;
        startInfo.Environment["BLUETOOTHMONITOR_E2E_FACADE_ASSEMBLY"] = typeof(AppLifecycle)
            .Assembly
            .Location;
        startInfo.Environment["BLUETOOTHMONITOR_E2E_FACADE_TYPE"] =
            "BluetoothMonitor.E2E.TestDoubles.FakeBluetoothFacade";

        _process =
            Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start BluetoothMonitor.exe");

        var pid = _process.Id;
        _application = Application.Attach(pid);

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(45);
        Window? window = null;
        while (DateTime.UtcNow < deadline)
        {
            if (HasProcessExited(pid))
            {
                // MSIX host may leave a sibling process; keep looking by name.
                if (Process.GetProcessesByName("BluetoothMonitor").Length == 0)
                {
                    throw new InvalidOperationException(
                        $"BluetoothMonitor.exe exited early (pid {pid}). "
                            + "MSIX packaging may block bare-exe launch; check deploy output."
                    );
                }
            }

            try
            {
                window = _application.GetMainWindow(_automation, TimeSpan.FromSeconds(1));
                if (window is not null && !string.IsNullOrWhiteSpace(window.Title))
                {
                    break;
                }

                foreach (var proc in Process.GetProcessesByName("BluetoothMonitor"))
                {
                    try
                    {
                        var attached = Application.Attach(proc.Id);
                        var candidate = attached.GetMainWindow(
                            _automation,
                            TimeSpan.FromSeconds(1)
                        );
                        if (
                            candidate is not null
                            && candidate.Title.Contains(
                                "Bluetooth Monitor",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            _application?.Dispose();
                            _application = attached;
                            window = candidate;
                            break;
                        }

                        attached.Dispose();
                    }
                    catch
                    {
                        // keep polling
                    }
                }

                if (window is not null)
                {
                    break;
                }

                var desktop = _automation.GetDesktop();
                window = desktop
                    .FindFirstDescendant(cf =>
                        cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window)
                            .And(cf.ByName("Bluetooth Monitor"))
                    )
                    ?.AsWindow();
                if (window is not null)
                {
                    break;
                }
            }
            catch
            {
                // keep polling
            }

            Thread.Sleep(250);
        }

        MainWindow =
            window
            ?? throw new TimeoutException(
                "Main Bluetooth Monitor window did not appear within 45s."
            );

        MainWindow.SetForeground();
        Thread.Sleep(500);
    }

    private static bool HasProcessExited(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            process.Dispose();
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private static string ResolveExePath()
    {
        var config = "Debug";
        var tfm = "net8.0-windows10.0.22621.0";
        var repoRoot = FindRepoRoot();
        var candidates = new[]
        {
            Path.Combine(
                repoRoot,
                "BluetoothMonitor.App",
                "bin",
                "x64",
                config,
                tfm,
                "win-x64",
                "BluetoothMonitor.exe"
            ),
            Path.Combine(
                repoRoot,
                "BluetoothMonitor.App",
                "bin",
                "x64",
                config,
                tfm,
                "BluetoothMonitor.exe"
            ),
            Path.Combine(
                repoRoot,
                "BluetoothMonitor.App",
                "bin",
                config,
                tfm,
                "win-x64",
                "BluetoothMonitor.exe"
            ),
            Path.Combine(
                repoRoot,
                "BluetoothMonitor.App",
                "bin",
                config,
                tfm,
                "BluetoothMonitor.exe"
            ),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return candidates[0];
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "BluetoothMonitor.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate BluetoothMonitor.sln from " + AppContext.BaseDirectory
        );
    }

    private static void KillLeftoverProcesses()
    {
        foreach (var process in Process.GetProcessesByName("BluetoothMonitor"))
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(3000);
            }
            catch
            {
                // ignored
            }
            finally
            {
                process.Dispose();
            }
        }
    }
}
