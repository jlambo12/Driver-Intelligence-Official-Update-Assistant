using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Infrastructure.DiagnosticLogging;

public sealed class NoOpDiagnosticLogger : IDiagnosticLogger
{
    public Task LogInfoAsync(string eventName, string message, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task LogWarningAsync(string eventName, string message, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task LogErrorAsync(string eventName, string message, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;
}
