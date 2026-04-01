using DriverGuardian.Application.Logging.Enums;

namespace DriverGuardian.Application.Logging.Models;

public sealed record LogEntry(
    DateTimeOffset TimestampUtc,
    AppLogLevel Level,
    LogCategory Category,
    string EventCode,
    string Message,
    string Source,
    OperationContext? OperationContext,
    SafeLogMetadata Metadata,
    string? ExceptionType = null,
    string? ExceptionMessage = null);
