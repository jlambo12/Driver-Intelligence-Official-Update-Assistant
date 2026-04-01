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
    Guid ScanSessionId);
