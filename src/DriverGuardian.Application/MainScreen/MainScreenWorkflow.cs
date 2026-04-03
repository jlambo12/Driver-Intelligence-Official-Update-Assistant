using DriverGuardian.Application.Abstractions;
using DriverGuardian.Contracts.DeviceDiscovery;

namespace DriverGuardian.Application.MainScreen;

public sealed class MainScreenWorkflow(
    IScanOrchestrator scanOrchestrator,
    IRecommendationPipeline recommendationPipeline,
    IProviderCatalogSummaryService providerCatalogSummaryService,
    ISettingsRepository settingsRepository,
    IDiagnosticLogger diagnosticLogger,
    IAuditWriter auditWriter,
    RecommendationDetailAssembler recommendationDetailAssembler,
    OfficialSourceActionService officialSourceActionService,
    ReportPayloadFactory reportPayloadFactory,
    HistoryWriter historyWriter,
    HistorySummarizer historySummarizer) : IMainScreenWorkflow
{
    public async Task<MainScreenWorkflowResult> RunScanAsync(CancellationToken cancellationToken)
    {
        try
        {
            await diagnosticLogger.LogInfoAsync("scan.workflow.start", "Запущен workflow анализа.", cancellationToken);
            var scanResult = await scanOrchestrator.RunAsync(cancellationToken);
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

            var recommendations = await recommendationPipeline.BuildAsync(scanResult.Drivers, cancellationToken);
            var recommendedCount = recommendations.Count(r => r.HasRecommendation);
            var notRecommendedCount = recommendations.Count - recommendedCount;
            await diagnosticLogger.LogInfoAsync(
                "scan.recommendation.completed",
                $"Рекомендации: всего {recommendations.Count}; к ручному действию {recommendedCount}; отложено {notRecommendedCount}.",
                cancellationToken);

            var providerCount = await providerCatalogSummaryService.GetProviderCountAsync(cancellationToken);
            var settings = await settingsRepository.GetAsync(cancellationToken);
            var recommendationDetails = recommendationDetailAssembler.Assemble(scanResult.DiscoveredDevices, scanResult.Drivers, recommendations);
            var manualHandoffReadyCount = recommendationDetails.Count(detail => detail.ManualHandoffReady);
            var manualHandoffUserActionCount = recommendationDetails.Count(detail => detail.ManualActionRequired);
            var officialSourceAction = await officialSourceActionService.BuildAsync(scanResult.Drivers, recommendations, cancellationToken);
            await diagnosticLogger.LogInfoAsync(
                "scan.official_source.state",
                officialSourceAction.IsReady
                    ? "Официальный источник подтверждён и доступен."
                    : $"Официальный источник заблокирован: {officialSourceAction.BlockReason ?? "причина не указана"}.",
                cancellationToken);

            var verificationSummary = BuildVerificationSummary(recommendationDetails);
            var reportExportPayload = reportPayloadFactory.Create(scanResult, recommendations);

            await historyWriter.WriteAsync(
                scanResult,
                recommendations.Count,
                manualHandoffUserActionCount,
                notRecommendedCount,
                verificationSummary,
                settings,
                cancellationToken);
            await diagnosticLogger.LogInfoAsync("scan.history_report.completed", "История и данные отчёта обновлены.", cancellationToken);

            var recentHistory = await historySummarizer.GetRecentAsync(settings.History.MaxEntries, cancellationToken);

            await auditWriter.WriteAsync($"scan:{scanResult.Session.Id}", cancellationToken);
            await diagnosticLogger.LogInfoAsync(
                "scan.workflow.summary",
                $"Сеанс {scanResult.Session.Id}; устройств {scanResult.DiscoveredDeviceCount}; драйверов {scanResult.Drivers.Count}; рекомендаций {recommendedCount}.",
                cancellationToken);

            return new MainScreenWorkflowResult(
                ScanExecutionStatus: scanResult.ExecutionStatus,
                ScanIssues: scanResult.Issues,
                DiscoveredDeviceCount: scanResult.DiscoveredDeviceCount,
                InspectedDriverCount: scanResult.Drivers.Count,
                RecommendedCount: recommendedCount,
                NotRecommendedCount: notRecommendedCount,
                ProviderCount: providerCount,
                ManualHandoffReadyCount: manualHandoffReadyCount,
                ManualHandoffUserActionCount: manualHandoffUserActionCount,
                VerificationSummary: verificationSummary,
                UiCulture: settings.UiCulture,
                ScanSessionId: scanResult.Session.Id,
                ReportExportPayload: reportExportPayload,
                RecommendationDetails: recommendationDetails,
                OfficialSourceAction: officialSourceAction,
                RecentHistory: recentHistory);
        }
        catch (Exception ex)
        {
            await diagnosticLogger.LogErrorAsync("scan.workflow.failed", "Ошибка выполнения workflow анализа.", ex, cancellationToken);
            throw;
        }
    }

    private static string BuildVerificationSummary(IReadOnlyCollection<RecommendationDetailResult> recommendationDetails)
    {
        var waitingForReturnCount = recommendationDetails.Count(detail => detail.VerificationAvailable);
        return waitingForReturnCount > 0
            ? $"Ожидается возврат пользователя по {waitingForReturnCount} устройств(ам). После ручной установки вернитесь и запустите повторный анализ: проверка будет доступна сразу."
            : "Действие не требуется: активных задач на возврат для проверки нет.";
    }
}
