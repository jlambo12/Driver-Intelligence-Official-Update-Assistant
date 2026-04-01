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
    IReadOnlyCollection<RecommendationDetailResult> RecommendationDetails,
    OpenOfficialSourceActionResult OfficialSourceAction,
    VerificationReturnResult VerificationReturn);

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
    bool ManualStepCompleted,
    bool VerificationPending);

public sealed record OpenOfficialSourceActionResult(
    bool IsReady,
    string Status,
    string? ApprovedOfficialSourceUrl,
    string? BlockReason);

public sealed record VerificationReturnResult(
    bool IsReady,
    bool ManualCompletionRequired,
    bool VerificationPending,
    string LastVerificationSummary);
