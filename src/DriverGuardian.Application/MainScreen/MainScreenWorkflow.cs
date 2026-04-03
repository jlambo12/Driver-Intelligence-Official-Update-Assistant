using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Recommendations;
using DriverGuardian.Domain.Scanning;

namespace DriverGuardian.Application.MainScreen;

public sealed class MainScreenWorkflow(
    IScanOrchestrator scanOrchestrator,
    IRecommendationPipeline recommendationPipeline,
    IProviderCatalogSummaryService providerCatalogSummaryService,
    ISettingsRepository settingsRepository,
    IDiagnosticLogger diagnosticLogger,
    IAuditWriter auditWriter,
    MainScreenResultAssembler resultAssembler,
    ScanSessionHistoryService historyService) : IMainScreenWorkflow
{
    public async Task<MainScreenWorkflowResult> RunScanAsync(CancellationToken cancellationToken)
    {
        try
        {
            await diagnosticLogger.LogInfoAsync("scan.workflow.start", "Запущен workflow анализа.", cancellationToken);

            var scanResult = await scanOrchestrator.RunAsync(cancellationToken);
            await LogScanCompletedAsync(scanResult, cancellationToken);

            var recommendations = await recommendationPipeline.BuildAsync(scanResult.Drivers, cancellationToken);
            var recommendationStats = BuildRecommendationStats(recommendations);
            await LogRecommendationCompletedAsync(recommendations.Count, recommendationStats, cancellationToken);

            var assembled = await resultAssembler.AssembleAsync(scanResult, recommendations, cancellationToken);
            await LogOfficialSourceStateAsync(assembled.OfficialSourceAction, cancellationToken);

            var providerCount = await providerCatalogSummaryService.GetProviderCountAsync(cancellationToken);
            var settings = await settingsRepository.GetAsync(cancellationToken);

            await historyService.RecordAndTrimAsync(
                scanResult,
                recommendations.Count,
                assembled.DetailStats.ManualActionRequiredCount,
                recommendationStats.NotRecommendedCount,
                assembled.VerificationSummary,
                settings,
                cancellationToken);
            await diagnosticLogger.LogInfoAsync("scan.history_report.completed", "История и данные отчёта обновлены.", cancellationToken);

            var recentHistory = await historyService.GetRecentAsync(settings.History.MaxEntries, cancellationToken);

            await auditWriter.WriteAsync($"scan:{scanResult.Session.Id}", cancellationToken);
            await diagnosticLogger.LogInfoAsync(
                "scan.workflow.summary",
                $"Сеанс {scanResult.Session.Id}; устройств {scanResult.DiscoveredDeviceCount}; драйверов {scanResult.Drivers.Count}; рекомендаций {recommendationStats.RecommendedCount}.",
                cancellationToken);

            return new MainScreenWorkflowResult(
                ScanExecutionStatus: scanResult.ExecutionStatus,
                ScanIssues: scanResult.Issues,
                DiscoveredDeviceCount: scanResult.DiscoveredDeviceCount,
                InspectedDriverCount: scanResult.Drivers.Count,
                RecommendedCount: recommendationStats.RecommendedCount,
                NotRecommendedCount: recommendationStats.NotRecommendedCount,
                ProviderCount: providerCount,
                ManualHandoffReadyCount: assembled.DetailStats.ManualHandoffReadyCount,
                ManualHandoffUserActionCount: assembled.DetailStats.ManualActionRequiredCount,
                VerificationSummary: assembled.VerificationSummary,
                UiCulture: settings.UiCulture,
                ScanSessionId: scanResult.Session.Id,
                ReportExportPayload: assembled.ReportPayload,
                RecommendationDetails: assembled.RecommendationDetails,
                OfficialSourceAction: assembled.OfficialSourceAction,
                RecentHistory: recentHistory);
        }
        catch (Exception ex)
        {
            await diagnosticLogger.LogErrorAsync("scan.workflow.failed", "Ошибка выполнения workflow анализа.", ex, cancellationToken);
            throw;
        }
    }

    private async Task LogScanCompletedAsync(ScanResult scanResult, CancellationToken cancellationToken)
    {
        await diagnosticLogger.LogInfoAsync(
            "scan.discovery.completed",
            $"Обнаружено записей: {scanResult.DiscoveredDeviceCount}; уникальных устройств: {scanResult.DiscoveredDevices.Count}.",
            cancellationToken);
        await diagnosticLogger.LogInfoAsync(
            "scan.inspection.completed",
            $"Проинспектировано драйверов: {scanResult.Drivers.Count}.",
            cancellationToken);

        if (scanResult.ExecutionStatus is ScanExecutionStatus.Partial or ScanExecutionStatus.Failed)
        {
            await diagnosticLogger.LogWarningAsync(
                "scan.integrity.warning",
                $"Анализ завершён со статусом {scanResult.ExecutionStatus}; проблем: {scanResult.Issues.Count}.",
                cancellationToken);
        }
    }

    private async Task LogRecommendationCompletedAsync(
        int totalRecommendations,
        RecommendationStats stats,
        CancellationToken cancellationToken)
    {
        await diagnosticLogger.LogInfoAsync(
            "scan.recommendation.completed",
            $"Рекомендации: всего {totalRecommendations}; к ручному действию {stats.RecommendedCount}; отложено {stats.NotRecommendedCount}.",
            cancellationToken);
    }

    private async Task LogOfficialSourceStateAsync(OpenOfficialSourceActionResult officialSourceAction, CancellationToken cancellationToken)
    {
        await diagnosticLogger.LogInfoAsync(
            "scan.official_source.state",
            officialSourceAction.IsReady
                ? "Официальный источник подтверждён и доступен."
                : $"Официальный источник заблокирован: {officialSourceAction.BlockReason ?? "причина не указана"}.",
            cancellationToken);
    }

    private static RecommendationStats BuildRecommendationStats(IReadOnlyCollection<RecommendationSummary> recommendations)
    {
        var recommendedCount = recommendations.Count(r => r.HasRecommendation);
        return new RecommendationStats(recommendedCount, recommendations.Count - recommendedCount);
    }

    private sealed record RecommendationStats(int RecommendedCount, int NotRecommendedCount);
}
