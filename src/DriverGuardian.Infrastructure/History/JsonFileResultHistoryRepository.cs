using System.Text.Json;
using DriverGuardian.Application.History;
using DriverGuardian.Application.History.Models;

namespace DriverGuardian.Infrastructure.History;

public sealed class JsonFileResultHistoryRepository : IResultHistoryRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly object _gate = new();
    private readonly string _filePath;

    public JsonFileResultHistoryRepository(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A history file path is required.", nameof(filePath));
        }

        _filePath = filePath;
    }

    public Task SaveAsync(ResultHistoryEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var entries = LoadEntriesUnsafe(cancellationToken);
            entries.RemoveAll(existing => existing.Id == entry.Id);
            entries.Add(entry);
            SaveEntriesUnsafe(entries, cancellationToken);
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
            var ordered = LoadEntriesUnsafe(cancellationToken)
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
            var entries = LoadEntriesUnsafe(cancellationToken);
            if (entries.Count <= maxEntries)
            {
                return Task.CompletedTask;
            }

            var trimmed = entries
                .OrderByDescending(entry => entry.OccurredAtUtc)
                .ThenByDescending(entry => entry.Id)
                .Take(maxEntries)
                .ToList();

            SaveEntriesUnsafe(trimmed, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private List<ResultHistoryEntry> LoadEntriesUnsafe(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_filePath))
        {
            return [];
        }

        using var stream = File.OpenRead(_filePath);
        var stored = JsonSerializer.Deserialize<List<StoredHistoryEntry>>(stream, SerializerOptions) ?? [];

        return stored
            .Select(MapStoredToDomain)
            .Where(entry => entry is not null)
            .Cast<ResultHistoryEntry>()
            .ToList();
    }

    private void SaveEntriesUnsafe(IReadOnlyCollection<ResultHistoryEntry> entries, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var stored = entries
            .Select(MapDomainToStored)
            .ToArray();

        using var stream = File.Create(_filePath);
        JsonSerializer.Serialize(stream, stored, SerializerOptions);
    }

    private static StoredHistoryEntry MapDomainToStored(ResultHistoryEntry entry)
        => entry switch
        {
            ScanHistoryEntry scan => new StoredHistoryEntry(
                Id: scan.Id,
                Kind: "scan",
                OccurredAtUtc: scan.OccurredAtUtc,
                ScanSessionId: scan.ScanSessionId,
                DiscoveredDeviceCount: scan.DiscoveredDeviceCount,
                InspectedDriverCount: scan.InspectedDriverCount,
                TotalRecommendations: null,
                RequiresManualInstallCount: null,
                DeferredDecisionCount: null,
                VerificationStatus: null,
                Note: null),
            RecommendationSummaryHistoryEntry recommendation => new StoredHistoryEntry(
                Id: recommendation.Id,
                Kind: "recommendation",
                OccurredAtUtc: recommendation.OccurredAtUtc,
                ScanSessionId: recommendation.ScanSessionId,
                DiscoveredDeviceCount: null,
                InspectedDriverCount: null,
                TotalRecommendations: recommendation.TotalRecommendations,
                RequiresManualInstallCount: recommendation.RequiresManualInstallCount,
                DeferredDecisionCount: recommendation.DeferredDecisionCount,
                VerificationStatus: null,
                Note: null),
            VerificationHistoryEntry verification => new StoredHistoryEntry(
                Id: verification.Id,
                Kind: "verification",
                OccurredAtUtc: verification.OccurredAtUtc,
                ScanSessionId: verification.ScanSessionId,
                DiscoveredDeviceCount: null,
                InspectedDriverCount: null,
                TotalRecommendations: null,
                RequiresManualInstallCount: null,
                DeferredDecisionCount: null,
                VerificationStatus: verification.Status.ToString(),
                Note: verification.Note),
            _ => throw new InvalidOperationException($"Unsupported history entry type: {entry.GetType().Name}")
        };

    private static ResultHistoryEntry? MapStoredToDomain(StoredHistoryEntry entry)
    {
        try
        {
            return entry.Kind switch
            {
                "scan" when entry.DiscoveredDeviceCount.HasValue && entry.InspectedDriverCount.HasValue
                    => ScanHistoryEntry.Create(entry.Id, entry.OccurredAtUtc, entry.ScanSessionId, entry.DiscoveredDeviceCount.Value, entry.InspectedDriverCount.Value),
                "recommendation" when entry.TotalRecommendations.HasValue && entry.RequiresManualInstallCount.HasValue && entry.DeferredDecisionCount.HasValue
                    => RecommendationSummaryHistoryEntry.Create(entry.Id, entry.OccurredAtUtc, entry.ScanSessionId, entry.TotalRecommendations.Value, entry.RequiresManualInstallCount.Value, entry.DeferredDecisionCount.Value),
                "verification"
                    => VerificationHistoryEntry.Create(entry.Id, entry.OccurredAtUtc, entry.ScanSessionId, ParseStatus(entry.VerificationStatus), entry.Note),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static VerificationHistoryStatus ParseStatus(string? status)
        => status?.Trim().ToLowerInvariant() switch
        {
            "passed" => VerificationHistoryStatus.Passed,
            "failed" => VerificationHistoryStatus.Failed,
            "skipped" => VerificationHistoryStatus.Skipped,
            _ => VerificationHistoryStatus.Unknown
        };

    private sealed record StoredHistoryEntry(
        Guid Id,
        string Kind,
        DateTimeOffset OccurredAtUtc,
        Guid ScanSessionId,
        int? DiscoveredDeviceCount,
        int? InspectedDriverCount,
        int? TotalRecommendations,
        int? RequiresManualInstallCount,
        int? DeferredDecisionCount,
        string? VerificationStatus,
        string? Note);
}
