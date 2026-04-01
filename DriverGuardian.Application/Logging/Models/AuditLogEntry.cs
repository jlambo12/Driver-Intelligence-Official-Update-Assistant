using DriverGuardian.Application.Logging.Enums;

namespace DriverGuardian.Application.Logging.Models;

public sealed record AuditLogEntry(
    DateTimeOffset TimestampUtc,
    string EventCode,
    string Action,
    string Actor,
    string Source,
    OperationContext? OperationContext,
    SafeLogMetadata Metadata,
    LogCategory Category = LogCategory.Audit);
