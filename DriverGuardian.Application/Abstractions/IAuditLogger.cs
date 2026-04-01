using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Application.Abstractions;

public interface IAuditLogger
{
    Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
