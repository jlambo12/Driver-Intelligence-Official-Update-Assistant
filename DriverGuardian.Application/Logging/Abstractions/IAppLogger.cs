using DriverGuardian.Application.Logging.Enums;
using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Application.Logging.Abstractions;

public interface IAppLogger
{
    Task LogAsync(
        AppLogLevel level,
        LogCategory category,
        string eventCode,
        string message,
        string source,
        OperationContext? operationContext,
        SafeLogMetadata? metadata,
        Exception? exception,
        CancellationToken cancellationToken);
}
