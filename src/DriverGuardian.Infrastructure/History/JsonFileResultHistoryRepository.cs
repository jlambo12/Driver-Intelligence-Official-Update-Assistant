using DriverGuardian.Application.History;
using DriverGuardian.Application.History.Models;

namespace DriverGuardian.Infrastructure.History;

public sealed class JsonFileResultHistoryRepository : IResultHistoryRepository
{
    private readonly object _gate = new();
    private readonly JsonFileHistoryStorage _storage;

    public JsonFileResultHistoryRepository(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A history file path is required.", nameof(filePath));
        }

        _storage = new JsonFileHistoryStorage(filePath);
    }

    public Task SaveAsync(ResultHistoryEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var entries = _storage.Load(cancellationToken);
            entries.RemoveAll(existing => existing.Id == entry.Id);
            entries.Add(entry);
            _storage.Save(entries, cancellationToken);
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

        lock (_gate)
        {
            var ordered = _storage.Load(cancellationToken)
                .OrderByDescending(entry => entry.OccurredAtUtc)
                .ThenByDescending(entry => entry.Id)
                .Take(take)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<ResultHistoryEntry>>(ordered);
        }
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
            var entries = _storage.Load(cancellationToken);
            if (entries.Count <= maxEntries)
            {
                return Task.CompletedTask;
            }

            var trimmed = entries
                .OrderByDescending(entry => entry.OccurredAtUtc)
                .ThenByDescending(entry => entry.Id)
                .Take(maxEntries)
                .ToList();

            _storage.Save(trimmed, cancellationToken);
        }

        return Task.CompletedTask;
    }
}
