using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Application.Logging.Abstractions;

public interface IOperationContextFactory
{
    OperationContext Create(string operationName, string source, CorrelationId? correlationId = null, string? parentOperationId = null);
}
