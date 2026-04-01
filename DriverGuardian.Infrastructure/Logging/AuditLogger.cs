using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Entities;
using DriverGuardian.Infrastructure.Abstractions;
using Microsoft.Extensions.Logging;

namespace DriverGuardian.Infrastructure.Logging;

public sealed class AuditLogger(IAuditPersistence persistence, ILogger<AuditLogger> logger) : IAuditLogger
{
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation("Audit event {EventType} for session {SessionId}", auditEvent.EventType, auditEvent.SessionId);
        await persistence.AppendAsync(auditEvent, cancellationToken);
    }
}
