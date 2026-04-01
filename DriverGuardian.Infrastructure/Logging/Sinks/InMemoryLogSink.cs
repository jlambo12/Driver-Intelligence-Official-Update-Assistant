using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Infrastructure.Logging.Sinks;

public sealed class InMemoryLogSink : ILogSink
{
    private readonly List<LogEntry> _entries = [];

    public IReadOnlyCollection<LogEntry> Entries => _entries;

    public Task WriteAsync(LogEntry entry, CancellationToken cancellationToken)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }
}
