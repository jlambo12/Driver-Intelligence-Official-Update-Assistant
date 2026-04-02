using System.Diagnostics;
using System.Text;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Settings;

namespace DriverGuardian.Infrastructure.DiagnosticLogging;

public sealed class FileDiagnosticLogger(
    ISettingsRepository settingsRepository,
    string defaultLogDirectory) : IDiagnosticLogger
{
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public async Task LogInfoAsync(string eventName, string message, CancellationToken cancellationToken)
    {
        await WriteAsync("INFO", eventName, message, null, cancellationToken);
    }

    public async Task LogErrorAsync(string eventName, string message, Exception exception, CancellationToken cancellationToken)
    {
        await WriteAsync("ERROR", eventName, message, exception, cancellationToken);
    }

    public string GetEffectiveLogDirectory()
    {
        var settings = settingsRepository.GetAsync(CancellationToken.None).GetAwaiter().GetResult();
        return ResolveEffectiveLogDirectory(settings);
    }

    public bool TryOpenEffectiveLogDirectory()
    {
        try
        {
            var directory = GetEffectiveLogDirectory();
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task WriteAsync(string level, string eventName, string message, Exception? exception, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        if (!settings.DiagnosticLogging.Enabled)
        {
            return;
        }

        var logDirectory = ResolveEffectiveLogDirectory(settings);
        var logFilePath = Path.Combine(logDirectory, $"scan-{DateTime.UtcNow:yyyyMMdd}.log");
        var line = BuildLogLine(level, eventName, message, exception);

        Directory.CreateDirectory(logDirectory);

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(logFilePath, line, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private string ResolveEffectiveLogDirectory(AppSettings settings)
    {
        var customPath = settings.DiagnosticLogging.CustomFolderPath;
        return string.IsNullOrWhiteSpace(customPath)
            ? defaultLogDirectory
            : customPath;
    }

    private static string BuildLogLine(string level, string eventName, string message, Exception? exception)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var safeMessage = message.Replace(Environment.NewLine, " ").Trim();
        if (exception is null)
        {
            return $"[{timestamp}] [{level}] [{eventName}] {safeMessage}{Environment.NewLine}";
        }

        return $"[{timestamp}] [{level}] [{eventName}] {safeMessage} | {exception.GetType().Name}: {exception.Message}{Environment.NewLine}";
    }
}
