using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Models;
using DriverGuardian.Infrastructure.Abstractions;

namespace DriverGuardian.Infrastructure.Logging;

public sealed class CentralizedAppLogger(
    IEnumerable<ILogSink> sinks,
    IMetadataSanitizer metadataSanitizer,
    IClock clock) : IAppLogger
{
    public async Task LogAsync(LogMessage message, CancellationToken cancellationToken)
    {
        var entry = new LogEntry(
            TimestampUtc: clock.UtcNow,
            Level: message.Level,
            Category: message.Category,
            EventCode: message.EventCode,
            Message: message.Message,
            Source: message.Source,
            OperationContext: message.OperationContext,
            Metadata: metadataSanitizer.Sanitize(message.Metadata),
            ExceptionType: message.Exception?.GetType().FullName,
            ExceptionMessage: message.Exception?.Message);

        foreach (var sink in sinks)
        {
            await sink.WriteAsync(entry, cancellationToken);
        }
    }
}
