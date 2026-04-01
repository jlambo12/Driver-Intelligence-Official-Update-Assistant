using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Enums;
using DriverGuardian.Application.Logging.Models;
using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Application.Services;

public sealed class ScanOrchestrator(
    IDriverInspectionOrchestrator inspectionOrchestrator,
    IAuditLogger auditLogger,
    IAppLogger appLogger,
    IErrorNormalizer errorNormalizer,
    IOperationContextFactory operationContextFactory,
    IOperationContextAccessor operationContextAccessor,
    IAppClock clock) : IScanOrchestrator
{
    public async Task<ScanSession> RunScanAsync(CancellationToken cancellationToken)
    {
        var operation = operationContextFactory.Create("scan.run", nameof(ScanOrchestrator));
        operationContextAccessor.Current = operation;

        await appLogger.LogAsync(
            new LogMessage(
                Level: AppLogLevel.Information,
                Category: LogCategory.ScanPipeline,
                EventCode: "SCAN_STARTED",
                Message: "Driver scan started.",
                Source: nameof(ScanOrchestrator),
                OperationContext: operation,
                Metadata: new SafeLogMetadata(new Dictionary<string, string> { ["operationId"] = operation.OperationId })),
            cancellationToken);

        try
        {
            var sessionId = Guid.NewGuid();
            var started = clock.UtcNow;
            var snapshots = await inspectionOrchestrator.InspectAsync(cancellationToken);
            var completed = clock.UtcNow;

            var session = new ScanSession(sessionId, started, completed, snapshots, HasErrors: false);

            await auditLogger.WriteAsync(
                new AuditLogEntry(
                    TimestampUtc: completed,
                    EventCode: "AUDIT_SCAN_COMPLETED",
                    Action: "scan.completed",
                    Actor: "System",
                    Source: nameof(ScanOrchestrator),
                    OperationContext: operation with { ParentOperationId = sessionId.ToString("D") },
                    Metadata: new SafeLogMetadata(new Dictionary<string, string>
                    {
                        ["sessionId"] = sessionId.ToString("D"),
                        ["deviceCount"] = session.Snapshots.Count.ToString()
                    })),
                cancellationToken);

            await appLogger.LogAsync(
                new LogMessage(
                    Level: AppLogLevel.Information,
                    Category: LogCategory.ScanPipeline,
                    EventCode: "SCAN_COMPLETED",
                    Message: "Driver scan completed.",
                    Source: nameof(ScanOrchestrator),
                    OperationContext: operation,
                    Metadata: new SafeLogMetadata(new Dictionary<string, string> { ["sessionId"] = sessionId.ToString("D") })),
                cancellationToken);

            return session;
        }
        catch (Exception ex)
        {
            var normalized = errorNormalizer.Normalize(ex, nameof(ScanOrchestrator), operationContextAccessor.Current);

            await appLogger.LogAsync(
                new LogMessage(
                    Level: AppLogLevel.Error,
                    Category: LogCategory.UnexpectedException,
                    EventCode: normalized.ErrorCode,
                    Message: normalized.TechnicalSummary,
                    Source: normalized.Source,
                    OperationContext: normalized.OperationContext,
                    Metadata: normalized.DiagnosticsMetadata,
                    Exception: ex),
                cancellationToken);

            throw;
        }
        finally
        {
            operationContextAccessor.Current = null;
        }
    }
}
