# Application logging

- Use `ILogger<T>` from `Microsoft.Extensions.Logging`. Do not call Serilog directly in application services.
- Serilog writes structured, rolling logs to `%LOCALAPPDATA%\BluetoothMonitor\Logs\app-YYYYMMDD.log`.
- The default level is `Information`. Use `Debug` for developer detail, `Warning` for recoverable problems, `Error` for failed operations, and `Critical` for application failure.
- Use message templates with named properties, for example: `_logger.LogInformation("Scan found {DeviceCount} devices", count);` Do not build log messages with interpolation.
- Log application start, successful initialization, shutdown, unhandled exceptions, and service operation failures.
- Log exceptions with the exception argument: `_logger.LogError(ex, "Settings load failed");` Do not catch and ignore failures without a log.
- Do not log passwords, tokens, secrets, or unnecessary personal data. Do not log Bluetooth IDs, MAC addresses, or full device paths.
- Logs keep a maximum of 14 daily files. Each file rolls at 10 MB.
