using DriverGuardian.Application.History.Models;

namespace DriverGuardian.Application.History;

public interface IResultHistoryRepository
{
    Task SaveAsync(ResultHistoryEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ResultHistoryEntry>> GetRecentAsync(int take, CancellationToken cancellationToken);

    Task TrimToMaxEntriesAsync(int maxEntries, CancellationToken cancellationToken);
}
