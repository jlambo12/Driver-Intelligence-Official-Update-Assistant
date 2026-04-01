namespace DriverGuardian.Application.Logging.Models;

public sealed record OperationContext(
    CorrelationId CorrelationId,
    string OperationName,
    string Source,
    DateTimeOffset StartedAtUtc,
    string OperationId,
    string? ParentOperationId = null);
