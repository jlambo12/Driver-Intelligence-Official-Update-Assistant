using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Models;
using DriverGuardian.Domain.Entities;
using DriverGuardian.Infrastructure.Abstractions;

namespace DriverGuardian.Infrastructure.Logging;

public sealed class AuditLogger(
    IEnumerable<IAuditSink> sinks,
    IAuditPersistence persistence,
    IMetadataSanitizer metadataSanitizer) : IAuditLogger
{
    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        var sanitized = entry with { Metadata = metadataSanitizer.Sanitize(entry.Metadata) };

        foreach (var sink in sinks)
        {
            await sink.WriteAsync(sanitized, cancellationToken);
        }

        var eventPayload = System.Text.Json.JsonSerializer.Serialize(sanitized.Metadata.Values);
        var sessionId = Guid.TryParse(sanitized.OperationContext?.ParentOperationId, out var parsed) ? parsed : Guid.Empty;

        var auditEvent = new AuditEvent(
            EventId: Guid.NewGuid(),
            SessionId: sessionId,
            OccurredAtUtc: sanitized.TimestampUtc,
            EventType: sanitized.EventCode,
            DetailsJson: eventPayload,
            Actor: sanitized.Actor);

        await persistence.AppendAsync(auditEvent, cancellationToken);
    }
}
