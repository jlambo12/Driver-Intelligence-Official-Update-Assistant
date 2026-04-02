namespace DriverGuardian.Application.Abstractions;

public interface IDiagnosticLogger
{
    Task LogInfoAsync(string message, CancellationToken cancellationToken);
    Task LogErrorAsync(string message, Exception exception, CancellationToken cancellationToken);
}
