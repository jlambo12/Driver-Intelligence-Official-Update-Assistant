using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Infrastructure.Logging.Sinks;

public sealed class InMemoryLogSink : ILogSink, ILogDiagnosticsQuery
{
    private readonly List<LogEntry> _entries = [];

    public IReadOnlyCollection<LogEntry> Entries => _entries;

    public Task WriteAsync(LogEntry entry, CancellationToken cancellationToken)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<LogEntry> GetRecent(int take = 200)
        => _entries.OrderByDescending(x => x.TimestampUtc).Take(take).ToArray();
}
