using DriverGuardian.Application.History.Models;

namespace DriverGuardian.Infrastructure.History;

internal static class JsonFileHistoryEntryMapper
{
    public static StoredHistoryEntry MapDomainToStored(ResultHistoryEntry entry)
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

    public static ResultHistoryEntry? MapStoredToDomain(StoredHistoryEntry entry)
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
}

internal sealed record StoredHistoryEntry(
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
