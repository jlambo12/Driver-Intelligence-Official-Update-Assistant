using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Infrastructure.DiagnosticLogging;

public sealed class SettingsDiagnosticLogger(
    ISettingsRepository settingsRepository,
    string defaultLogsDirectory) : IDiagnosticLogger
{
    private readonly SemaphoreSlim _sync = new(1, 1);
    private IDiagnosticLogger? _cachedLogger;
    private bool _cachedEnabled;
    private string _cachedFolder = string.Empty;

    public async Task LogInfoAsync(string eventName, string message, CancellationToken cancellationToken)
    {
        var logger = await ResolveLoggerAsync(cancellationToken);
        await logger.LogInfoAsync(eventName, message, cancellationToken);
    }

    public async Task LogWarningAsync(string eventName, string message, CancellationToken cancellationToken)
    {
        var logger = await ResolveLoggerAsync(cancellationToken);
        await logger.LogWarningAsync(eventName, message, cancellationToken);
    }

    public async Task LogErrorAsync(string eventName, string message, Exception exception, CancellationToken cancellationToken)
    {
        var logger = await ResolveLoggerAsync(cancellationToken);
        await logger.LogErrorAsync(eventName, message, exception, cancellationToken);
    }

    private async Task<IDiagnosticLogger> ResolveLoggerAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        var enabled = settings.DiagnosticLogging.Enabled;
        var folder = string.IsNullOrWhiteSpace(settings.DiagnosticLogging.CustomLogsFolderPath)
            ? defaultLogsDirectory
            : settings.DiagnosticLogging.CustomLogsFolderPath.Trim();

        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_cachedLogger is not null && _cachedEnabled == enabled && StringComparer.Ordinal.Equals(_cachedFolder, folder))
            {
                return _cachedLogger;
            }

            _cachedEnabled = enabled;
            _cachedFolder = folder;
            _cachedLogger = enabled
                ? new FileDiagnosticLogger(folder)
                : new NoOpDiagnosticLogger();

            return _cachedLogger;
        }
        finally
        {
            _sync.Release();
        }
    }
}
