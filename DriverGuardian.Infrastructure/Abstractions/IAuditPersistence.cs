using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Infrastructure.Abstractions;

public interface IAuditPersistence
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
