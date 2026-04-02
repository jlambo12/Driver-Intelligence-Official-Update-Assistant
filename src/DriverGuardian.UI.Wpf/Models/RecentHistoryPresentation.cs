using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Localization;

namespace DriverGuardian.UI.Wpf.Models;

public sealed record RecentHistoryPresentation(
    string Timestamp,
    string Title,
    string Summary,
    string Status,
    string Guidance)
{
    public static IReadOnlyCollection<RecentHistoryPresentation> FromResults(IReadOnlyCollection<RecentHistoryEntryResult> entries)
        => entries.Select(Map).ToArray();

    private static RecentHistoryPresentation Map(RecentHistoryEntryResult entry)
    {
        var timestamp = entry.OccurredAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

        return entry.Kind switch
        {
            RecentHistoryEntryKind.Scan => new RecentHistoryPresentation(
                timestamp,
                UiStrings.RecentHistoryTypeScan,
                string.Format(UiStrings.RecentHistoryScanSummaryFormat, FormatScanSessionId(entry.ScanSessionId)),
                string.Format(UiStrings.RecentHistoryScanStatusFormat, entry.PrimaryCount, entry.SecondaryCount),
                UiStrings.RecentHistoryScanGuidance),
            RecentHistoryEntryKind.Recommendation => new RecentHistoryPresentation(
                timestamp,
                UiStrings.RecentHistoryTypeRecommendation,
                UiStrings.RecentHistoryRecommendationSummary,
                string.Format(UiStrings.RecentHistoryRecommendationStatusFormat, entry.PrimaryCount, entry.SecondaryCount, entry.TertiaryCount),
                UiStrings.RecentHistoryRecommendationGuidance),
            RecentHistoryEntryKind.Verification => new RecentHistoryPresentation(
                timestamp,
                UiStrings.RecentHistoryTypeVerification,
                UiStrings.RecentHistoryVerificationSummary,
                string.Format(UiStrings.RecentHistoryVerificationStatusFormat, MapVerificationStatus(entry.StatusCode), string.IsNullOrWhiteSpace(entry.Note) ? UiStrings.RecentHistoryVerificationNoteEmpty : entry.Note),
                UiStrings.RecentHistoryVerificationGuidance),
            _ => new RecentHistoryPresentation(
                timestamp,
                UiStrings.RecentHistoryTypeUnknown,
                UiStrings.RecentHistoryUnknownSummary,
                UiStrings.RecentHistoryUnknownStatus,
                UiStrings.RecentHistoryUnknownGuidance)
        };
    }

    private static string FormatScanSessionId(Guid scanSessionId)
        => $"#{scanSessionId.ToString("N")[..8]}";

    private static string MapVerificationStatus(string? statusCode)
        => statusCode switch
        {
            "passed" => UiStrings.RecentHistoryVerificationStatusPassed,
            "failed" => UiStrings.RecentHistoryVerificationStatusFailed,
            "skipped" => UiStrings.RecentHistoryVerificationStatusSkipped,
            _ => UiStrings.RecentHistoryVerificationStatusUnknown
        };
}
