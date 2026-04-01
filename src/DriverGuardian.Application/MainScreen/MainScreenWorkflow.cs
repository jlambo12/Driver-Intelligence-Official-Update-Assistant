using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Application.MainScreen;

public sealed class MainScreenWorkflow(
    IScanOrchestrator scanOrchestrator,
    IRecommendationPipeline recommendationPipeline,
    IProviderCatalogSummaryService providerCatalogSummaryService,
    ISettingsRepository settingsRepository,
    IAuditWriter auditWriter) : IMainScreenWorkflow
{
    public async Task<MainScreenWorkflowResult> RunScanAsync(CancellationToken cancellationToken)
    {
        var scanResult = await scanOrchestrator.RunAsync(cancellationToken);
        var recommendations = await recommendationPipeline.BuildAsync(scanResult.Drivers, cancellationToken);
        var providerCount = await providerCatalogSummaryService.GetProviderCountAsync(cancellationToken);
        var settings = await settingsRepository.GetAsync(cancellationToken);

        await auditWriter.WriteAsync($"scan:{scanResult.Session.Id}", cancellationToken);

        return new MainScreenWorkflowResult(
            DriverCount: scanResult.Drivers.Count,
            RecommendationCount: recommendations.Count(r => r.HasRecommendation),
            ProviderCount: providerCount,
            UiCulture: settings.UiCulture,
            ScanSessionId: scanResult.Session.Id);
    }
}
