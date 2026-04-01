using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Verification;

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
        var recommendedCount = recommendations.Count(r => r.HasRecommendation);
        var notRecommendedCount = recommendations.Count - recommendedCount;
        var manualHandoffReadyCount = 0;
        var manualHandoffUserActionCount = recommendedCount;
        var verificationPlaceholder = new PostInstallVerificationEvaluator()
            .Evaluate(new PostInstallVerificationRequest(
                scanResult.Drivers.FirstOrDefault()?.DeviceIdentity ?? new Domain.Devices.DeviceIdentity("UNKNOWN\\DEVICE\\0"),
                null,
                null))
            .Message;

        await auditWriter.WriteAsync($"scan:{scanResult.Session.Id}", cancellationToken);

        return new MainScreenWorkflowResult(
            DiscoveredDeviceCount: scanResult.DiscoveredDeviceCount,
            InspectedDriverCount: scanResult.Drivers.Count,
            RecommendedCount: recommendedCount,
            NotRecommendedCount: notRecommendedCount,
            ProviderCount: providerCount,
            ManualHandoffReadyCount: manualHandoffReadyCount,
            ManualHandoffUserActionCount: manualHandoffUserActionCount,
            VerificationSummary: verificationPlaceholder,
            UiCulture: settings.UiCulture,
            ScanSessionId: scanResult.Session.Id);
    }
}
