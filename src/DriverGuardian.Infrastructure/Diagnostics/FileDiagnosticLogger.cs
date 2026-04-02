using System.Text;
using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Infrastructure.Diagnostics;

public sealed class FileDiagnosticLogger(
    ISettingsRepository settingsRepository,
    ILogFolderResolver logFolderResolver) : IDiagnosticLogger
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task LogInfoAsync(string message, CancellationToken cancellationToken)
    {
        await LogInternalAsync("INFO", message, null, cancellationToken);
    }

    public async Task LogErrorAsync(string message, Exception exception, CancellationToken cancellationToken)
    {
        await LogInternalAsync("ERROR", message, exception, cancellationToken);
    }

    private async Task LogInternalAsync(string level, string message, Exception? exception, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var settings = await settingsRepository.GetAsync(cancellationToken);
        if (!settings.DiagnosticLogging.Enabled)
        {
            return;
        }

        var folder = await logFolderResolver.GetEffectiveLogFolderAsync(cancellationToken);
        var timestamp = DateTimeOffset.UtcNow;
        var filePath = Path.Combine(folder, $"scan-{timestamp:yyyyMMdd}.log");
        var line = BuildLine(level, timestamp, message.Trim(), exception);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(folder);
            await File.AppendAllTextAsync(filePath, line, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static string BuildLine(string level, DateTimeOffset timestamp, string message, Exception? exception)
    {
        if (exception is null)
        {
            return $"[{timestamp:O}] [{level}] {message}{Environment.NewLine}";
        }

        return $"[{timestamp:O}] [{level}] {message}; exception={exception.GetType().Name}; details={exception.Message}{Environment.NewLine}";
    }
}
