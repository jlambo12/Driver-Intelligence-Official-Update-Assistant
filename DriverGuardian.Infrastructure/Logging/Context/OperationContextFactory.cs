using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Models;
using DriverGuardian.Infrastructure.Abstractions;

namespace DriverGuardian.Infrastructure.Logging.Context;

public sealed class OperationContextFactory(IClock clock) : IOperationContextFactory
{
    public OperationContext Create(string operationName, string source, CorrelationId? correlationId = null, string? parentOperationId = null)
        => new(
            CorrelationId: correlationId ?? CorrelationId.Create(),
            OperationName: operationName,
            Source: source,
            StartedAtUtc: clock.UtcNow,
            OperationId: Guid.NewGuid().ToString("N"),
            ParentOperationId: parentOperationId);
}
