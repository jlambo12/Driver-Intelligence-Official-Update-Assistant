using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Application.Abstractions;

public interface IAuditLogger
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken);
}
