namespace DriverGuardian.Application.Abstractions;

public interface IMainScreenWorkflow
{
    Task<MainScreenWorkflowResult> RunScanAsync(CancellationToken cancellationToken);
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
    OpenOfficialSourceActionResult OfficialSourceAction);

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
