using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Services;
using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class ScanOrchestratorTests
{
    [Fact]
    public async Task RunScanAsync_Should_WriteAuditEvent()
    {
        var inspector = new FakeInspectionOrchestrator();
        var audit = new FakeAuditLogger();
        var sut = new ScanOrchestrator(inspector, audit);

        var result = await sut.RunScanAsync(CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.SessionId);
        Assert.Single(audit.Events);
    }

    private sealed class FakeInspectionOrchestrator : IDriverInspectionOrchestrator
    {
        public Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<InstalledDriverSnapshot>>([]);
    }

    private sealed class FakeAuditLogger : IAuditLogger
    {
        public List<AuditEvent> Events { get; } = [];

        public Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}
