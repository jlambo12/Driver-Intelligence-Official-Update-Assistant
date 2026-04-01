using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Application.Logging.Abstractions;

public interface IAuditSink
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken);
}
