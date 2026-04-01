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
    IOperationContextAccessor operationContextAccessor) : IScanOrchestrator
{
    public async Task<ScanSession> RunScanAsync(CancellationToken cancellationToken)
    {
        var operation = operationContextFactory.Create("scan.run", nameof(ScanOrchestrator));
        operationContextAccessor.Current = operation;

        await appLogger.LogAsync(
            AppLogLevel.Information,
            LogCategory.ScanPipeline,
            "SCAN_STARTED",
            "Driver scan started.",
            nameof(ScanOrchestrator),
            operation,
            new SafeLogMetadata(new Dictionary<string, string> { ["operationId"] = operation.EffectiveOperationId }),
            null,
            cancellationToken);

        try
        {
            var sessionId = Guid.NewGuid();
            var started = DateTimeOffset.UtcNow;
            var snapshots = await inspectionOrchestrator.InspectAsync(cancellationToken);
            var completed = DateTimeOffset.UtcNow;

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
                AppLogLevel.Information,
                LogCategory.ScanPipeline,
                "SCAN_COMPLETED",
                "Driver scan completed.",
                nameof(ScanOrchestrator),
                operation,
                new SafeLogMetadata(new Dictionary<string, string> { ["sessionId"] = sessionId.ToString("D") }),
                null,
                cancellationToken);

            return session;
        }
        catch (Exception ex)
        {
            var normalized = errorNormalizer.Normalize(ex, nameof(ScanOrchestrator), operationContextAccessor.Current);

            await appLogger.LogAsync(
                AppLogLevel.Error,
                LogCategory.UnexpectedException,
                normalized.ErrorCode,
                normalized.TechnicalSummary,
                normalized.Source,
                normalized.OperationContext,
                normalized.DiagnosticsMetadata,
                ex,
                cancellationToken);

            throw;
        }
        finally
        {
            operationContextAccessor.Current = null;
        }
    }
}
