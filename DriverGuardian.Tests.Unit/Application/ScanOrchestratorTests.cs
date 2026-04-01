using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Enums;
using DriverGuardian.Application.Logging.Models;
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
        var appLogger = new FakeAppLogger();
        var errorNormalizer = new FakeErrorNormalizer();
        var contextFactory = new FakeOperationContextFactory();
        var contextAccessor = new FakeOperationContextAccessor();
        var clock = new FakeClock();

        var sut = new ScanOrchestrator(inspector, audit, appLogger, errorNormalizer, contextFactory, contextAccessor, clock);

        var result = await sut.RunScanAsync(CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.SessionId);
        Assert.Single(audit.Events);
        Assert.True(appLogger.Entries.Count >= 2);
        Assert.All(appLogger.Entries, entry => Assert.NotNull(entry.OperationContext?.OperationId));
    }

    private sealed class FakeInspectionOrchestrator : IDriverInspectionOrchestrator
    {
        public Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<InstalledDriverSnapshot>>([]);
    }

    private sealed class FakeAuditLogger : IAuditLogger
    {
        public List<AuditLogEntry> Events { get; } = [];

        public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
        {
            Events.Add(entry);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAppLogger : IAppLogger
    {
        public List<LogMessage> Entries { get; } = [];

        public Task LogAsync(LogMessage message, CancellationToken cancellationToken)
        {
            Entries.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeErrorNormalizer : IErrorNormalizer
    {
        public NormalizedAppError Normalize(Exception exception, string source, OperationContext? operationContext, SafeLogMetadata? metadata = null)
            => new(DateTimeOffset.UtcNow, "TEST", ErrorCategory.Unknown, ErrorSeverity.Error, RecoverabilityHint.Unknown, source, operationContext, "Errors.TEST", exception.Message, metadata ?? SafeLogMetadata.Empty);
    }

    private sealed class FakeOperationContextFactory : IOperationContextFactory
    {
        public OperationContext Create(string operationName, string source, CorrelationId? correlationId = null, string? parentOperationId = null)
            => new(correlationId ?? CorrelationId.Create(), operationName, source, DateTimeOffset.UtcNow, Guid.NewGuid().ToString("N"), parentOperationId);
    }

    private sealed class FakeOperationContextAccessor : IOperationContextAccessor
    {
        public OperationContext? Current { get; set; }
    }

    private sealed class FakeClock : IAppClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-01-01T00:00:00+00:00");
    }
}
