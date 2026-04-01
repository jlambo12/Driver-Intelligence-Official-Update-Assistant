namespace DriverGuardian.Domain.Entities;

public sealed record AuditEvent(
    Guid EventId,
    Guid SessionId,
    DateTimeOffset OccurredAtUtc,
    string EventType,
    string DetailsJson,
    string Actor = "System");
