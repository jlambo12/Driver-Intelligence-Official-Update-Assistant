namespace DriverGuardian.Application.Logging.Models;

public sealed record OperationContext(
    CorrelationId CorrelationId,
    string OperationName,
    string Source,
    DateTimeOffset StartedAtUtc,
    string? ParentOperationId = null,
    string? OperationId = null)
{
    public string EffectiveOperationId => OperationId ?? Guid.NewGuid().ToString("N");
}
