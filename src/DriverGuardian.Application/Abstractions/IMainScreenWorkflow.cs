using DriverGuardian.Application.History.Models;

namespace DriverGuardian.Application.Abstractions;

public interface IMainScreenWorkflow
{
    Task<MainScreenWorkflowResult> RunScanAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RecentHistoryEntryResult>> GetRecentHistoryAsync(int take, CancellationToken cancellationToken);
}

public sealed record MainScreenWorkflowResult(
    int DiscoveredDeviceCount,
    int InspectedDriverCount,
    int RecommendedCount,
    int NotRecommendedCount,
    int ProviderCount,
    int ManualHandoffReadyCount,
    int ManualHandoffUserActionCount,
    string VerificationSummary,
    string UiCulture,
    Guid ScanSessionId,
    ReportExportPayload ReportExportPayload,
    IReadOnlyCollection<RecommendationDetailResult> RecommendationDetails,
    OpenOfficialSourceActionResult OfficialSourceAction,
    IReadOnlyCollection<RecentHistoryEntryResult> RecentHistory);

public sealed record ReportExportPayload(
    string FileNameBase,
    string PlainTextContent,
    string MarkdownContent);

public sealed record RecommendationDetailResult(
    string DeviceId,
    bool HasRecommendation,
    string RecommendationReason,
    string InstalledVersion,
    string? InstalledProvider,
    string? RecommendedVersion,
    bool ManualHandoffReady,
    bool ManualActionRequired,
    bool VerificationAvailable,
    string VerificationStatus);

public sealed record OpenOfficialSourceActionResult(
    bool IsReady,
    string Status,
    string? ApprovedOfficialSourceUrl,
    string? BlockReason);

public sealed record RecentHistoryEntryResult(
    DateTimeOffset OccurredAtUtc,
    RecentHistoryEntryKind Kind,
    Guid ScanSessionId,
    int FirstValue,
    int SecondValue,
    int ThirdValue,
    VerificationHistoryStatus? VerificationStatus,
    string? VerificationNote);

public enum RecentHistoryEntryKind
{
    Scan = 0,
    RecommendationSummary = 1,
    Verification = 2,
    Unknown = 3
}
