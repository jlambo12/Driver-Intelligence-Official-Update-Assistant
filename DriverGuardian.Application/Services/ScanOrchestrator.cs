using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Application.Services;

public sealed class ScanOrchestrator(
    IDriverInspectionOrchestrator inspectionOrchestrator,
    IAuditLogger auditLogger) : IScanOrchestrator
{
    public async Task<ScanSession> RunScanAsync(CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid();
        var started = DateTimeOffset.UtcNow;
        var snapshots = await inspectionOrchestrator.InspectAsync(cancellationToken);
        var completed = DateTimeOffset.UtcNow;

        var session = new ScanSession(sessionId, started, completed, snapshots, HasErrors: false);

        await auditLogger.WriteAsync(
            new AuditEvent(Guid.NewGuid(), sessionId, completed, "ScanCompleted", "{}"),
            cancellationToken);

        return session;
    }
}
