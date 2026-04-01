using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Application.Logging.Abstractions;

public interface IErrorNormalizer
{
    NormalizedAppError Normalize(Exception exception, string source, OperationContext? operationContext, SafeLogMetadata? metadata = null);
}
