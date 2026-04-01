namespace DriverGuardian.Application.Abstractions;

public interface IMainScreenWorkflow
{
    Task<MainScreenWorkflowResult> RunScanAsync(CancellationToken cancellationToken);
}

public sealed record MainScreenWorkflowResult(
    int DriverCount,
    int RecommendationCount,
    int ProviderCount,
    string UiCulture,
    Guid ScanSessionId);
