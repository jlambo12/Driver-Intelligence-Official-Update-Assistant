using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Infrastructure.DiagnosticLogging;

public sealed class NoOpDiagnosticLogger(string defaultLogDirectory) : IDiagnosticLogger
{
    public Task LogInfoAsync(string eventName, string message, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task LogErrorAsync(string eventName, string message, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;

    public string GetEffectiveLogDirectory() => defaultLogDirectory;

    public bool TryOpenEffectiveLogDirectory()
    {
        try
        {
            Directory.CreateDirectory(defaultLogDirectory);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
