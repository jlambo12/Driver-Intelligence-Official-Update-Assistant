using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Contracts.DeviceDiscovery;

namespace DriverGuardian.Application.Abstractions;

public interface IMainScreenWorkflow
{
    Task<MainScreenWorkflowResult> RunScanAsync(CancellationToken cancellationToken);
}

public sealed record MainScreenWorkflowResult(
    ScanExecutionStatus ScanExecutionStatus,
    IReadOnlyCollection<ScanIssue> ScanIssues,
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

public enum RecommendationWorkflowState
{
    NoActionRequired = 0,
    RecommendationAvailable = 1,
    ManualActionRequired = 2,
    AwaitingVerification = 3
}

public sealed record RecommendationDetailResult(
    string DeviceDisplayName,
    string DeviceId,
    int PriorityBucket,
    RecommendationWorkflowState WorkflowState,
    string RecommendationReason,
    string InstalledVersion,
    string? InstalledProvider,
    string? RecommendedVersion,
    string VerificationStatus)
{
    public bool HasRecommendation => WorkflowState != RecommendationWorkflowState.NoActionRequired;

    public bool IsManualHandoffReady => WorkflowState is RecommendationWorkflowState.ManualActionRequired or RecommendationWorkflowState.AwaitingVerification;

    public bool IsManualActionRequired => WorkflowState == RecommendationWorkflowState.ManualActionRequired;

    public bool IsAwaitingVerification => WorkflowState == RecommendationWorkflowState.AwaitingVerification;
}

public sealed record OpenOfficialSourceActionResult(
    bool IsReady,
    OfficialSourceResolutionOutcome ResolutionOutcome,
    OfficialSourceActionTarget ActionTarget,
    string Status,
    string? ApprovedOfficialSourceUrl,
    string? BlockReason);

public enum OfficialSourceActionTarget
{
    SourcePage = 0,
    DirectDownloadPage = 1
}

public sealed record RecentHistoryEntryResult(
    DateTimeOffset OccurredAtUtc,
    RecentHistoryEntryKind Kind,
    Guid ScanSessionId,
    int PrimaryCount,
    int SecondaryCount,
    int TertiaryCount,
    string? StatusCode,
    string? Note);

public enum RecentHistoryEntryKind
{
    Scan = 0,
    Recommendation = 1,
    Verification = 2,
    Unknown = 3
}
