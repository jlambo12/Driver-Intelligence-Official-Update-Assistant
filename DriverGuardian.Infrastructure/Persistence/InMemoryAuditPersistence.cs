using DriverGuardian.Domain.Entities;
using DriverGuardian.Infrastructure.Abstractions;

namespace DriverGuardian.Infrastructure.Persistence;

public sealed class InMemoryAuditPersistence : IAuditPersistence
{
    private readonly List<AuditEvent> _events = [];

    public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        _events.Add(auditEvent);
        return Task.CompletedTask;
    }
}
