using DriverGuardian.Application.Logging.Enums;

namespace DriverGuardian.Application.Logging.Models;

public sealed record LogMessage(
    AppLogLevel Level,
    LogCategory Category,
    string EventCode,
    string Message,
    string Source,
    OperationContext? OperationContext,
    SafeLogMetadata? Metadata = null,
    Exception? Exception = null);
