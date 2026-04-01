namespace DriverGuardian.Application.History.Models;

public sealed record RecommendationSummaryHistoryEntry : ResultHistoryEntry
{
    private RecommendationSummaryHistoryEntry(
        Guid id,
        DateTimeOffset occurredAtUtc,
        Guid scanSessionId,
        int totalRecommendations,
        int requiresManualInstallCount,
        int deferredDecisionCount)
        : base(id, occurredAtUtc)
    {
        ScanSessionId = scanSessionId;
        TotalRecommendations = totalRecommendations;
        RequiresManualInstallCount = requiresManualInstallCount;
        DeferredDecisionCount = deferredDecisionCount;
    }

    public Guid ScanSessionId { get; }

    public int TotalRecommendations { get; }

    public int RequiresManualInstallCount { get; }

    public int DeferredDecisionCount { get; }

    public static RecommendationSummaryHistoryEntry Create(
        Guid id,
        DateTimeOffset occurredAtUtc,
        Guid scanSessionId,
        int totalRecommendations,
        int requiresManualInstallCount,
        int deferredDecisionCount)
    {
        if (scanSessionId == Guid.Empty)
        {
            throw new ArgumentException("Scan session identifier cannot be empty.", nameof(scanSessionId));
        }

        if (totalRecommendations < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalRecommendations), "Total recommendations cannot be negative.");
        }

        if (requiresManualInstallCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiresManualInstallCount), "Manual install count cannot be negative.");
        }

        if (deferredDecisionCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deferredDecisionCount), "Deferred decision count cannot be negative.");
        }

        if (requiresManualInstallCount > totalRecommendations)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiresManualInstallCount),
                "Manual install count cannot exceed total recommendations.");
        }

        if (deferredDecisionCount > totalRecommendations)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deferredDecisionCount),
                "Deferred decision count cannot exceed total recommendations.");
        }

        return new RecommendationSummaryHistoryEntry(
            id,
            occurredAtUtc,
            scanSessionId,
            totalRecommendations,
            requiresManualInstallCount,
            deferredDecisionCount);
    }
}
