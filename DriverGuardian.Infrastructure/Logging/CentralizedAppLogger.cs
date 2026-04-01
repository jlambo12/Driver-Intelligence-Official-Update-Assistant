using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Enums;
using DriverGuardian.Application.Logging.Models;
using DriverGuardian.Infrastructure.Abstractions;

namespace DriverGuardian.Infrastructure.Logging;

public sealed class CentralizedAppLogger(
    IEnumerable<ILogSink> sinks,
    IMetadataSanitizer metadataSanitizer,
    IClock clock) : IAppLogger
{
    public async Task LogAsync(
        AppLogLevel level,
        LogCategory category,
        string eventCode,
        string message,
        string source,
        OperationContext? operationContext,
        SafeLogMetadata? metadata,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        var entry = new LogEntry(
            TimestampUtc: clock.UtcNow,
            Level: level,
            Category: category,
            EventCode: eventCode,
            Message: message,
            Source: source,
            OperationContext: operationContext,
            Metadata: metadataSanitizer.Sanitize(metadata),
            ExceptionType: exception?.GetType().FullName,
            ExceptionMessage: exception?.Message);

        foreach (var sink in sinks)
        {
            await sink.WriteAsync(entry, cancellationToken);
        }
    }
}
