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

        var sut = new ScanOrchestrator(inspector, audit, appLogger, errorNormalizer, contextFactory, contextAccessor);

        var result = await sut.RunScanAsync(CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.SessionId);
        Assert.Single(audit.Events);
        Assert.True(appLogger.Entries.Count >= 2);
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
        public List<LogEntry> Entries { get; } = [];

        public Task LogAsync(AppLogLevel level, LogCategory category, string eventCode, string message, string source, OperationContext? operationContext, SafeLogMetadata? metadata, Exception? exception, CancellationToken cancellationToken)
        {
            Entries.Add(new LogEntry(DateTimeOffset.UtcNow, level, category, eventCode, message, source, operationContext, metadata ?? SafeLogMetadata.Empty));
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
            => new(correlationId ?? CorrelationId.Create(), operationName, source, DateTimeOffset.UtcNow, parentOperationId, Guid.NewGuid().ToString("N"));
    }

    private sealed class FakeOperationContextAccessor : IOperationContextAccessor
    {
        public OperationContext? Current { get; set; }
    }
}
