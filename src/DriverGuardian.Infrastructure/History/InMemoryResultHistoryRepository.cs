using DriverGuardian.Application.History;
using DriverGuardian.Application.History.Models;

namespace DriverGuardian.Infrastructure.History;

public sealed class InMemoryResultHistoryRepository : IResultHistoryRepository
{
    private readonly object _gate = new();
    private readonly List<ResultHistoryEntry> _entries = [];

    public Task SaveAsync(ResultHistoryEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _entries.RemoveAll(existing => existing.Id == entry.Id);
            _entries.Add(entry);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ResultHistoryEntry>> GetRecentAsync(int take, CancellationToken cancellationToken)
    {
        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        ResultHistoryEntry[] ordered;
        lock (_gate)
        {
            ordered = _entries
                .OrderByDescending(entry => entry.OccurredAtUtc)
                .ThenByDescending(entry => entry.Id)
                .Take(take)
                .ToArray();
        }

        return Task.FromResult<IReadOnlyCollection<ResultHistoryEntry>>(ordered);
    }

    public Task TrimToMaxEntriesAsync(int maxEntries, CancellationToken cancellationToken)
    {
        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries), "Max entries must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_entries.Count <= maxEntries)
            {
                return Task.CompletedTask;
            }

            var retainedIds = _entries
                .OrderByDescending(entry => entry.OccurredAtUtc)
                .ThenByDescending(entry => entry.Id)
                .Take(maxEntries)
                .Select(entry => entry.Id)
                .ToHashSet();

            _entries.RemoveAll(entry => !retainedIds.Contains(entry.Id));
        }

        return Task.CompletedTask;
    }
}
