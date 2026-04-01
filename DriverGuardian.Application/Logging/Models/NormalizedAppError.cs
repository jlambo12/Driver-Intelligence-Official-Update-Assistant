using DriverGuardian.Application.Logging.Enums;

namespace DriverGuardian.Application.Logging.Models;

public sealed record NormalizedAppError(
    DateTimeOffset TimestampUtc,
    string ErrorCode,
    ErrorCategory Category,
    ErrorSeverity Severity,
    RecoverabilityHint Recoverability,
    string Source,
    OperationContext? OperationContext,
    string UserMessageKey,
    string TechnicalSummary,
    SafeLogMetadata DiagnosticsMetadata,
    string? ExceptionType = null,
    string? ExceptionMessage = null,
    string? ExceptionStackTrace = null);
