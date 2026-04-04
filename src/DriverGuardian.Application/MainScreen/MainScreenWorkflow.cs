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
            await LogRecommendationCompletedAsync(scanResult.Session.Id, recommendations.Count, recommendationStats, cancellationToken);

            var assembled = await resultAssembler.AssembleAsync(scanResult, recommendations, cancellationToken);
            await LogOfficialSourceStateAsync(scanResult.Session.Id, assembled.OfficialSourceAction, cancellationToken);

            var providerCount = await providerCatalogSummaryService.GetProviderCountAsync(cancellationToken);
            var settings = await settingsRepository.GetAsync(cancellationToken);

            await historyService.RecordAndTrimAsync(
                scanResult,
                recommendations.Count,
                assembled.DetailStats.ManualActionRequiredCount,
                recommendationStats.NotRecommendedCount,
                assembled.Verifications,
                assembled.VerificationSummary,
                settings,
                cancellationToken);
            await diagnosticLogger.LogInfoAsync("scan.history_report.completed", "История и данные отчёта обновлены.", cancellationToken);

            var recentHistory = await historyService.GetRecentAsync(settings.History.MaxEntries, cancellationToken);

            await auditWriter.WriteAsync(
                BuildAuditEntry(
                    scanResult.Session.Id,
                    "scan.completed",
                    $"devices={scanResult.DiscoveredDeviceCount};drivers={scanResult.Drivers.Count};recommended={recommendationStats.RecommendedCount};status={scanResult.ExecutionStatus}"),
                cancellationToken);
            await auditWriter.WriteAsync(
                BuildAuditEntry(
                    scanResult.Session.Id,
                    "official_source.state",
                    $"ready={assembled.OfficialSourceAction.IsReady};resolution={assembled.OfficialSourceAction.ResolutionOutcome};target={assembled.OfficialSourceAction.ActionTarget}"),
                cancellationToken);
            await auditWriter.WriteAsync(
                BuildAuditEntry(
                    scanResult.Session.Id,
                    "verification.summary",
                    $"manual_action_required={assembled.DetailStats.ManualActionRequiredCount};summary={SanitizeForAudit(assembled.VerificationSummary)}"),
                cancellationToken);
            await diagnosticLogger.LogInfoAsync(
                "scan.workflow.summary",
                $"session={scanResult.Session.Id}; устройств {scanResult.DiscoveredDeviceCount}; драйверов {scanResult.Drivers.Count}; рекомендаций {recommendationStats.RecommendedCount}.",
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
            $"session={scanResult.Session.Id}; обнаружено записей: {scanResult.DiscoveredDeviceCount}; уникальных устройств: {scanResult.DiscoveredDevices.Count}.",
            cancellationToken);
        await diagnosticLogger.LogInfoAsync(
            "scan.inspection.completed",
            $"session={scanResult.Session.Id}; проинспектировано драйверов: {scanResult.Drivers.Count}.",
            cancellationToken);

        if (scanResult.ExecutionStatus is ScanExecutionStatus.Partial or ScanExecutionStatus.Failed)
        {
            await diagnosticLogger.LogWarningAsync(
                "scan.integrity.warning",
                $"session={scanResult.Session.Id}; анализ завершён со статусом {scanResult.ExecutionStatus}; проблем: {scanResult.Issues.Count}.",
                cancellationToken);
        }
    }

    private async Task LogRecommendationCompletedAsync(
        Guid sessionId,
        int totalRecommendations,
        RecommendationStats stats,
        CancellationToken cancellationToken)
    {
        await diagnosticLogger.LogInfoAsync(
            "scan.recommendation.completed",
            $"session={sessionId}; рекомендации: всего {totalRecommendations}; к ручному действию {stats.RecommendedCount}; отложено {stats.NotRecommendedCount}.",
            cancellationToken);
    }

    private async Task LogOfficialSourceStateAsync(Guid sessionId, OpenOfficialSourceActionResult officialSourceAction, CancellationToken cancellationToken)
    {
        await diagnosticLogger.LogInfoAsync(
            "scan.official_source.state",
            officialSourceAction.IsReady
                ? $"session={sessionId}; официальный источник подтверждён и доступен."
                : $"session={sessionId}; официальный источник заблокирован: {officialSourceAction.BlockReason ?? "причина не указана"}.",
            cancellationToken);
    }

    private static RecommendationStats BuildRecommendationStats(IReadOnlyCollection<RecommendationSummary> recommendations)
    {
        var recommendedCount = recommendations.Count(r => r.HasRecommendation);
        return new RecommendationStats(recommendedCount, recommendations.Count - recommendedCount);
    }

    private static string BuildAuditEntry(Guid sessionId, string eventName, string payload)
        => $"session={sessionId};event={eventName};{payload}";

    private static string SanitizeForAudit(string value)
        => string.IsNullOrWhiteSpace(value)
            ? "none"
            : value.Replace(';', ',').Replace(Environment.NewLine, " ", StringComparison.Ordinal);

    private sealed record RecommendationStats(int RecommendedCount, int NotRecommendedCount);
}
