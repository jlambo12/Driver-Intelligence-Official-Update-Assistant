namespace DriverGuardian.Application.Abstractions;

public interface IDiagnosticLogger
{
    Task LogInfoAsync(string eventName, string message, CancellationToken cancellationToken);
    Task LogWarningAsync(string eventName, string message, CancellationToken cancellationToken);
    Task LogErrorAsync(string eventName, string message, Exception exception, CancellationToken cancellationToken);
}
