using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Infrastructure.Logging.Sinks;

public sealed class InMemoryAuditSink : IAuditSink
{
    private readonly List<AuditLogEntry> _entries = [];

    public IReadOnlyCollection<AuditLogEntry> Entries => _entries;

    public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }
}
