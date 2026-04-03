using DriverGuardian.Application.Abstractions;
using DriverGuardian.Infrastructure.DiagnosticLogging;

namespace DriverGuardian.UI.Wpf.Services;

public sealed class RuntimeDiagnosticLogger(
    ISettingsRepository settingsRepository,
    string defaultLogsDirectory) : IDiagnosticLogger
{
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
        if (!settings.DiagnosticLogging.Enabled)
        {
            return new NoOpDiagnosticLogger();
        }

        var folder = string.IsNullOrWhiteSpace(settings.DiagnosticLogging.CustomLogsFolderPath)
            ? defaultLogsDirectory
            : settings.DiagnosticLogging.CustomLogsFolderPath.Trim();

        return new FileDiagnosticLogger(folder);
    }
}
