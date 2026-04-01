using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.History;
using DriverGuardian.Application.History.Models;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Application.Reports;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.MainScreen;

public sealed class MainScreenWorkflow(
    IScanOrchestrator scanOrchestrator,
    IRecommendationPipeline recommendationPipeline,
    IProviderCatalogSummaryService providerCatalogSummaryService,
    ISettingsRepository settingsRepository,
    IAuditWriter auditWriter,
    IResultHistoryRepository resultHistoryRepository,
    OpenOfficialSourceActionEvaluator openOfficialSourceActionEvaluator,
    IShareableReportBuilder reportBuilder) : IMainScreenWorkflow
{
    private const int DefaultRecentHistoryTake = 5;

    public async Task<MainScreenWorkflowResult> RunScanAsync(CancellationToken cancellationToken)
    {
        var scanResult = await scanOrchestrator.RunAsync(cancellationToken);
        var recommendations = await recommendationPipeline.BuildAsync(scanResult.Drivers, cancellationToken);
        var providerCount = await providerCatalogSummaryService.GetProviderCountAsync(cancellationToken);
        var settings = await settingsRepository.GetAsync(cancellationToken);
        var recommendedCount = recommendations.Count(r => r.HasRecommendation);
        var notRecommendedCount = recommendations.Count - recommendedCount;
        var recommendationDetails = BuildRecommendationDetails(scanResult.Drivers, recommendations);
        var manualHandoffReadyCount = recommendationDetails.Count(detail => detail.ManualHandoffReady);
        var manualHandoffUserActionCount = recommendationDetails.Count(detail => detail.ManualActionRequired);
        var officialSourceAction = BuildOfficialSourceAction(recommendationDetails, openOfficialSourceActionEvaluator);
        var verificationSummary = BuildVerificationSummary(recommendationDetails);
        var reportExportPayload = BuildReportExportPayload(scanResult, recommendations, reportBuilder);

        var occurredAtUtc = scanResult.Session.CompletedAtUtc ?? DateTimeOffset.UtcNow;
        await resultHistoryRepository.SaveAsync(
            ScanHistoryEntry.Create(Guid.NewGuid(), occurredAtUtc, scanResult.Session.Id, scanResult.DiscoveredDeviceCount, scanResult.Drivers.Count),
            cancellationToken);
        await resultHistoryRepository.SaveAsync(
            RecommendationSummaryHistoryEntry.Create(
                Guid.NewGuid(),
                occurredAtUtc,
                scanResult.Session.Id,
                recommendations.Count,
                manualHandoffUserActionCount,
                notRecommendedCount),
            cancellationToken);
        await resultHistoryRepository.SaveAsync(
            VerificationHistoryEntry.Create(
                Guid.NewGuid(),
                occurredAtUtc,
                scanResult.Session.Id,
                manualHandoffUserActionCount > 0 ? VerificationHistoryStatus.Skipped : VerificationHistoryStatus.Passed,
                verificationSummary),
            cancellationToken);

        var recentHistory = await GetRecentHistoryAsync(DefaultRecentHistoryTake, cancellationToken);

        await auditWriter.WriteAsync($"scan:{scanResult.Session.Id}", cancellationToken);

        return new MainScreenWorkflowResult(
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

    public async Task<IReadOnlyCollection<RecentHistoryEntryResult>> GetRecentHistoryAsync(int take, CancellationToken cancellationToken)
    {
        var recentEntries = await resultHistoryRepository.GetRecentAsync(take, cancellationToken);
        return recentEntries.Select(MapHistory).ToArray();
    }

    private static RecentHistoryEntryResult MapHistory(ResultHistoryEntry entry)
        => entry switch
        {
            ScanHistoryEntry scan => new RecentHistoryEntryResult(
                scan.OccurredAtUtc,
                RecentHistoryEntryKind.Scan,
                scan.ScanSessionId,
                scan.DiscoveredDeviceCount,
                scan.InspectedDriverCount,
                0,
                null,
                null),
            RecommendationSummaryHistoryEntry recommendation => new RecentHistoryEntryResult(
                recommendation.OccurredAtUtc,
                RecentHistoryEntryKind.RecommendationSummary,
                recommendation.ScanSessionId,
                recommendation.TotalRecommendations,
                recommendation.RequiresManualInstallCount,
                recommendation.DeferredDecisionCount,
                null,
                null),
            VerificationHistoryEntry verification => new RecentHistoryEntryResult(
                verification.OccurredAtUtc,
                RecentHistoryEntryKind.Verification,
                verification.ScanSessionId,
                0,
                0,
                0,
                verification.Status,
                verification.Note),
            _ => new RecentHistoryEntryResult(
                entry.OccurredAtUtc,
                RecentHistoryEntryKind.Unknown,
                Guid.Empty,
                0,
                0,
                0,
                null,
                null)
        };

    private static ReportExportPayload BuildReportExportPayload(
        ScanResult scanResult,
        IReadOnlyCollection<Domain.Recommendations.RecommendationSummary> recommendations,
        IShareableReportBuilder reportBuilder)
    {
        var generatedAtUtc = DateTimeOffset.UtcNow;
        var report = reportBuilder.Build(
            new ShareableReportRequest(
                scanResult,
                recommendations,
                [],
                [],
                generatedAtUtc));
        var plainTextContent = reportBuilder.BuildStructuredText(report);
        var markdownContent = $"# DriverGuardian Scan Report{Environment.NewLine}{Environment.NewLine}```text{Environment.NewLine}{plainTextContent}{Environment.NewLine}```";
        var fileNameBase = $"driverguardian-report-{scanResult.Session.Id:N}";

        return new ReportExportPayload(fileNameBase, plainTextContent, markdownContent);
    }

    private static IReadOnlyCollection<RecommendationDetailResult> BuildRecommendationDetails(
        IReadOnlyCollection<Domain.Drivers.InstalledDriverSnapshot> drivers,
        IReadOnlyCollection<Domain.Recommendations.RecommendationSummary> recommendations)
    {
        var byDevice = recommendations.ToDictionary(item => item.DeviceIdentity.Value, StringComparer.OrdinalIgnoreCase);

        return drivers.Select(driver =>
            {
                var hasRecommendation = byDevice.TryGetValue(driver.DeviceIdentity.Value, out var recommendation) && recommendation.HasRecommendation;

                return new RecommendationDetailResult(
                    DeviceId: driver.DeviceIdentity.Value,
                    HasRecommendation: hasRecommendation,
                    RecommendationReason: recommendation?.Reason ?? string.Empty,
                    InstalledVersion: driver.DriverVersion,
                    InstalledProvider: driver.ProviderName,
                    RecommendedVersion: recommendation?.RecommendedVersion,
                    ManualHandoffReady: false,
                    ManualActionRequired: hasRecommendation,
                    VerificationAvailable: hasRecommendation,
                    VerificationStatus: hasRecommendation
                        ? "Ожидается возврат пользователя после ручной установки."
                        : "Проверка после установки сейчас не требуется.");
            })
            .ToArray();
    }

    private static string BuildVerificationSummary(IReadOnlyCollection<RecommendationDetailResult> recommendationDetails)
    {
        var waitingForReturnCount = recommendationDetails.Count(detail => detail.VerificationAvailable);
        return waitingForReturnCount > 0
            ? $"Ожидается возврат для проверки по устройствам: {waitingForReturnCount}."
            : "Активных задач на возврат для проверки нет.";
    }

    private static OpenOfficialSourceActionResult BuildOfficialSourceAction(
        IReadOnlyCollection<RecommendationDetailResult> recommendationDetails,
        OpenOfficialSourceActionEvaluator openOfficialSourceActionEvaluator)
    {
        var targetRecommendation = recommendationDetails.FirstOrDefault(item => item.ManualActionRequired);
        if (targetRecommendation is null)
        {
            return new OpenOfficialSourceActionResult(
                IsReady: false,
                Status: "Нет рекомендаций для перехода к официальному источнику.",
                ApprovedOfficialSourceUrl: null,
                BlockReason: null);
        }

        var sourceEvidence = new SourceEvidence(
            new Uri("https://pending.official-source.local"),
            "Не определено",
            SourceTrustLevel.Unknown,
            false,
            "Требуется подтверждение официального источника пользователем.");

        var decision = openOfficialSourceActionEvaluator.Evaluate(
            new OpenOfficialSourceActionRequest("pending-provider", targetRecommendation.DeviceId, sourceEvidence, null));

        return new OpenOfficialSourceActionResult(
            IsReady: decision.IsAllowed,
            Status: decision.IsAllowed
                ? "Официальный источник подтверждён для ручного перехода."
                : "Открытие официального источника требует ручной проверки.",
            ApprovedOfficialSourceUrl: decision.Link?.OfficialSourceUri.ToString(),
            BlockReason: decision.Blockers.FirstOrDefault()?.Reason.ToString());
    }
}
