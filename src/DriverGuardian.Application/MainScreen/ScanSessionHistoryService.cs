using DriverGuardian.Application.History;
using DriverGuardian.Application.History.Models;
using DriverGuardian.Domain.Settings;
using DriverGuardian.Domain.Scanning;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Reports;
using DriverGuardian.Application.Verification;

namespace DriverGuardian.Application.MainScreen;

public sealed class ScanSessionHistoryService(IResultHistoryRepository resultHistoryRepository)
{
    public async Task RecordAndTrimAsync(
        ScanResult scanResult,
        int recommendationCount,
        int manualHandoffUserActionCount,
        int notRecommendedCount,
        IReadOnlyCollection<VerificationReportItem> verifications,
        string verificationSummary,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var occurredAtUtc = scanResult.Session.CompletedAtUtc ?? DateTimeOffset.UtcNow;
        await resultHistoryRepository.SaveAsync(
            ScanHistoryEntry.Create(Guid.NewGuid(), occurredAtUtc, scanResult.Session.Id, scanResult.DiscoveredDeviceCount, scanResult.Drivers.Count),
            cancellationToken);
        await resultHistoryRepository.SaveAsync(
            RecommendationSummaryHistoryEntry.Create(
                Guid.NewGuid(),
                occurredAtUtc,
                scanResult.Session.Id,
                Math.Max(recommendationCount, manualHandoffUserActionCount),
                manualHandoffUserActionCount,
                notRecommendedCount),
            cancellationToken);
        await resultHistoryRepository.SaveAsync(
            VerificationHistoryEntry.Create(
                Guid.NewGuid(),
                occurredAtUtc,
                scanResult.Session.Id,
                ResolveVerificationStatus(verifications, manualHandoffUserActionCount),
                verificationSummary),
            cancellationToken);

        await resultHistoryRepository.TrimToMaxEntriesAsync(settings.History.MaxEntries, cancellationToken);
    }

    public async Task<IReadOnlyCollection<RecentHistoryEntryResult>> GetRecentAsync(int maxEntries, CancellationToken cancellationToken)
    {
        var recentHistoryEntries = await resultHistoryRepository.GetRecentAsync(maxEntries, cancellationToken);
        return recentHistoryEntries.Select(MapHistoryEntry).ToArray();
    }

    private static RecentHistoryEntryResult MapHistoryEntry(ResultHistoryEntry entry)
        => entry switch
        {
            ScanHistoryEntry scan => new RecentHistoryEntryResult(
                scan.OccurredAtUtc,
                RecentHistoryEntryKind.Scan,
                scan.ScanSessionId,
                scan.DiscoveredDeviceCount,
                scan.InspectedDriverCount,
                0,
                null,
                null),
            RecommendationSummaryHistoryEntry recommendation => new RecentHistoryEntryResult(
                recommendation.OccurredAtUtc,
                RecentHistoryEntryKind.Recommendation,
                recommendation.ScanSessionId,
                recommendation.TotalRecommendations,
                recommendation.RequiresManualInstallCount,
                recommendation.DeferredDecisionCount,
                null,
                null),
            VerificationHistoryEntry verification => new RecentHistoryEntryResult(
                verification.OccurredAtUtc,
                RecentHistoryEntryKind.Verification,
                verification.ScanSessionId,
                0,
                0,
                0,
                MapVerificationStatusCode(verification.Status),
                verification.Note),
            _ => new RecentHistoryEntryResult(entry.OccurredAtUtc, RecentHistoryEntryKind.Unknown, Guid.Empty, 0, 0, 0, null, null)
        };

    private static string MapVerificationStatusCode(VerificationHistoryStatus status)
        => status switch
        {
            VerificationHistoryStatus.Passed => "passed",
            VerificationHistoryStatus.Failed => "failed",
            VerificationHistoryStatus.Skipped => "skipped",
            _ => "unknown"
        };

    private static VerificationHistoryStatus ResolveVerificationStatus(
        IReadOnlyCollection<VerificationReportItem> verifications,
        int manualHandoffUserActionCount)
    {
        if (verifications.Count == 0)
        {
            return manualHandoffUserActionCount > 0
                ? VerificationHistoryStatus.Skipped
                : VerificationHistoryStatus.Passed;
        }

        var hasFailureLikeOutcome = verifications.Any(item =>
            item.Result.Outcome is PostInstallVerificationOutcome.NoChangeDetected
                or PostInstallVerificationOutcome.DeviceMissing
                or PostInstallVerificationOutcome.InsufficientEvidence);

        return hasFailureLikeOutcome
            ? VerificationHistoryStatus.Failed
            : VerificationHistoryStatus.Passed;
    }
}
