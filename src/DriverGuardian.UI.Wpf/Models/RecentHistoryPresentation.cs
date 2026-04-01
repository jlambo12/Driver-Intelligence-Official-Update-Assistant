using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.History.Models;
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
                string.Format(UiStrings.RecentHistoryScanStatusFormat, entry.FirstValue, entry.SecondValue)),
            RecentHistoryEntryKind.RecommendationSummary => new RecentHistoryPresentation(
                timestamp,
                UiStrings.RecentHistoryTypeRecommendation,
                UiStrings.RecentHistoryRecommendationSummaryTitle,
                string.Format(UiStrings.RecentHistoryRecommendationStatusFormat, entry.FirstValue, entry.SecondValue, entry.ThirdValue)),
            RecentHistoryEntryKind.Verification => new RecentHistoryPresentation(
                timestamp,
                UiStrings.RecentHistoryTypeVerification,
                UiStrings.RecentHistoryVerificationSummaryTitle,
                string.Format(
                    UiStrings.RecentHistoryVerificationStatusFormat,
                    MapVerificationStatus(entry.VerificationStatus),
                    string.IsNullOrWhiteSpace(entry.VerificationNote) ? UiStrings.RecentHistoryVerificationNoteEmpty : entry.VerificationNote)),
            _ => new RecentHistoryPresentation(
                timestamp,
                UiStrings.RecentHistoryTypeUnknown,
                UiStrings.RecentHistoryUnknownSummary,
                UiStrings.RecentHistoryUnknownStatus)
        };
    }

    private static string MapVerificationStatus(VerificationHistoryStatus? status)
        => status switch
        {
            VerificationHistoryStatus.Passed => UiStrings.RecentHistoryVerificationStatusPassed,
            VerificationHistoryStatus.Failed => UiStrings.RecentHistoryVerificationStatusFailed,
            VerificationHistoryStatus.Skipped => UiStrings.RecentHistoryVerificationStatusSkipped,
            _ => UiStrings.RecentHistoryVerificationStatusUnknown
        };
}
