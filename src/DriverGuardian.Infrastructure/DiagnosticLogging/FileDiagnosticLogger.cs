using System.Text;
using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Infrastructure.DiagnosticLogging;

public sealed class FileDiagnosticLogger(string logsDirectoryPath) : IDiagnosticLogger
{
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public Task LogInfoAsync(string eventName, string message, CancellationToken cancellationToken)
        => WriteAsync("INFO", eventName, message, null, cancellationToken);

    public Task LogWarningAsync(string eventName, string message, CancellationToken cancellationToken)
        => WriteAsync("WARN", eventName, message, null, cancellationToken);

    public Task LogErrorAsync(string eventName, string message, Exception exception, CancellationToken cancellationToken)
        => WriteAsync("ERROR", eventName, message, exception, cancellationToken);

    private async Task WriteAsync(
        string level,
        string eventName,
        string message,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logsDirectoryPath))
        {
            return;
        }

        Directory.CreateDirectory(logsDirectoryPath);
        var filePath = Path.Combine(logsDirectoryPath, $"scan-{DateTime.UtcNow:yyyyMMdd}.log");
        var line = BuildLine(level, eventName, message, exception);

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(filePath, line, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private static string BuildLine(string level, string eventName, string message, Exception? exception)
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
