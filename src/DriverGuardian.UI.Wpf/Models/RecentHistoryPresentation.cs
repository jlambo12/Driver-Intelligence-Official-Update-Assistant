using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Localization;

namespace DriverGuardian.UI.Wpf.Models;

public sealed record RecentHistoryPresentation(
    string Timestamp,
    string Title,
    string Summary,
    string Status)
{
    public static IReadOnlyCollection<RecentHistoryPresentation> FromResults(IReadOnlyCollection<RecentHistoryEntryResult> entries)
        => entries.Select(Map).ToArray();

    private static RecentHistoryPresentation Map(RecentHistoryEntryResult entry)
    {
        var timestamp = entry.OccurredAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        return entry.Kind switch
        {
            RecentHistoryEntryKind.Scan => new RecentHistoryPresentation(
                timestamp,
                UiStrings.RecentHistoryTypeScan,
                string.Format(UiStrings.RecentHistoryScanSummaryFormat, entry.ScanSessionId),
                string.Format(UiStrings.RecentHistoryScanStatusFormat, entry.PrimaryCount, entry.SecondaryCount)),
            RecentHistoryEntryKind.Recommendation => new RecentHistoryPresentation(
                timestamp,
                UiStrings.RecentHistoryTypeRecommendation,
                UiStrings.RecentHistoryRecommendationSummary,
                string.Format(UiStrings.RecentHistoryRecommendationStatusFormat, entry.PrimaryCount, entry.SecondaryCount, entry.TertiaryCount)),
            RecentHistoryEntryKind.Verification => new RecentHistoryPresentation(
                timestamp,
                UiStrings.RecentHistoryTypeVerification,
                UiStrings.RecentHistoryVerificationSummary,
                string.Format(UiStrings.RecentHistoryVerificationStatusFormat, MapVerificationStatus(entry.StatusCode), string.IsNullOrWhiteSpace(entry.Note) ? UiStrings.RecentHistoryVerificationNoteEmpty : entry.Note)),
            _ => new RecentHistoryPresentation(timestamp, UiStrings.RecentHistoryTypeUnknown, UiStrings.RecentHistoryUnknownSummary, UiStrings.RecentHistoryUnknownStatus)
        };
    }

    private static string MapVerificationStatus(string? statusCode)
        => statusCode switch
        {
            "passed" => UiStrings.RecentHistoryVerificationStatusPassed,
            "failed" => UiStrings.RecentHistoryVerificationStatusFailed,
            "skipped" => UiStrings.RecentHistoryVerificationStatusSkipped,
            _ => UiStrings.RecentHistoryVerificationStatusUnknown
        };
}
