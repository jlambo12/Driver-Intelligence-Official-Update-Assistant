using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Infrastructure.Audit;

public sealed class InMemoryAuditWriter : IAuditWriter
{
    private readonly List<string> _entries = [];

    public IReadOnlyCollection<string> Entries => _entries;

    public Task WriteAsync(string entry, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(entry))
        {
            _entries.Add(entry.Trim());
        }

        return Task.CompletedTask;
    }
}
