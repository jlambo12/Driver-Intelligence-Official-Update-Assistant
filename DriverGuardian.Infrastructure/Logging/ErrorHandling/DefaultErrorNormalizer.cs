using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Enums;
using DriverGuardian.Application.Logging.Models;
using DriverGuardian.Infrastructure.Abstractions;

namespace DriverGuardian.Infrastructure.Logging.ErrorHandling;

public sealed class DefaultErrorNormalizer(IClock clock, IMetadataSanitizer metadataSanitizer) : IErrorNormalizer
{
    public NormalizedAppError Normalize(Exception exception, string source, OperationContext? operationContext, SafeLogMetadata? metadata = null)
    {
        var (errorCode, category, severity, recoverability) = exception switch
        {
            ArgumentException => ("VALIDATION_ERROR", ErrorCategory.Validation, ErrorSeverity.Warning, RecoverabilityHint.UserActionRequired),
            InvalidOperationException => ("INVALID_OPERATION", ErrorCategory.Operation, ErrorSeverity.Error, RecoverabilityHint.Retryable),
            _ => ("UNEXPECTED_ERROR", ErrorCategory.Unexpected, ErrorSeverity.Critical, RecoverabilityHint.Unknown)
        };

        return new NormalizedAppError(
            TimestampUtc: clock.UtcNow,
            ErrorCode: errorCode,
            Category: category,
            Severity: severity,
            Recoverability: recoverability,
            Source: source,
            OperationContext: operationContext,
            UserMessageKey: $"Errors.{errorCode}",
            TechnicalSummary: exception.Message,
            DiagnosticsMetadata: metadataSanitizer.Sanitize(metadata),
            ExceptionType: exception.GetType().FullName,
            ExceptionMessage: exception.Message,
            ExceptionStackTrace: exception.StackTrace);
    }
}
