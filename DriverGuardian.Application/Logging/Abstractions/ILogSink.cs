using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Application.Logging.Abstractions;

public interface ILogSink
{
    Task WriteAsync(LogEntry entry, CancellationToken cancellationToken);
}
