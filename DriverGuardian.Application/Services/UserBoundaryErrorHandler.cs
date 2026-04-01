using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Enums;
using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Application.Services;

public sealed class UserBoundaryErrorHandler(
    IErrorNormalizer errorNormalizer,
    IAppLogger appLogger,
    IOperationContextAccessor operationContextAccessor) : IUserBoundaryErrorHandler
{
    public Task HandleAsync(Exception exception, string source, CancellationToken cancellationToken)
    {
        var normalized = errorNormalizer.Normalize(exception, source, operationContextAccessor.Current);

        return appLogger.LogAsync(
            new LogMessage(
                Level: AppLogLevel.Error,
                Category: LogCategory.UnexpectedException,
                EventCode: normalized.ErrorCode,
                Message: normalized.TechnicalSummary,
                Source: normalized.Source,
                OperationContext: normalized.OperationContext,
                Metadata: normalized.DiagnosticsMetadata,
                Exception: exception),
            cancellationToken);
    }
}
