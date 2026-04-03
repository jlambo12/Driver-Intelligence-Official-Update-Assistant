using DriverGuardian.Domain.Recommendations;
using DriverGuardian.Domain.Scanning;
using DriverGuardian.Application.Reports;
using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Application.MainScreen;

public sealed class MainScreenResultAssembler(
    RecommendationDetailAssembler recommendationDetailAssembler,
    OfficialSourceActionService officialSourceActionService,
    IShareableReportBuilder reportBuilder)
{
    public async Task<MainScreenAssembledResult> AssembleAsync(
        ScanResult scanResult,
        IReadOnlyCollection<RecommendationSummary> recommendations,
        CancellationToken cancellationToken)
    {
        var recommendationDetails = recommendationDetailAssembler.Assemble(scanResult.DiscoveredDevices, scanResult.Drivers, recommendations);
        var detailStats = new RecommendationDetailStats(
            recommendationDetails.Count(detail => detail.ManualHandoffReady),
            recommendationDetails.Count(detail => detail.ManualActionRequired));

        var officialSourceAction = await officialSourceActionService.BuildAsync(scanResult.Drivers, recommendations, cancellationToken);
        var verificationSummary = BuildVerificationSummary(recommendationDetails);

        var generatedAtUtc = DateTimeOffset.UtcNow;
        var report = reportBuilder.Build(
            new ShareableReportRequest(
                scanResult,
                recommendations,
                [],
                [],
                generatedAtUtc));

        var plainTextContent = reportBuilder.BuildStructuredText(report);
        var markdownContent = $"# DriverGuardian Shareable Scan Report{Environment.NewLine}{Environment.NewLine}```text{Environment.NewLine}{plainTextContent}{Environment.NewLine}```";
        var reportPayload = new ReportExportPayload(
            BuildReportFileNameBase(scanResult.Session.Id, generatedAtUtc),
            plainTextContent,
            markdownContent);

        return new MainScreenAssembledResult(
            recommendationDetails,
            detailStats,
            verificationSummary,
            reportPayload,
            officialSourceAction);
    }

    private static string BuildVerificationSummary(IReadOnlyCollection<RecommendationDetailResult> recommendationDetails)
    {
        var waitingForReturnCount = recommendationDetails.Count(detail => detail.VerificationAvailable);
        return waitingForReturnCount > 0
            ? $"Ожидается возврат пользователя по {waitingForReturnCount} устройств(ам). После ручной установки вернитесь и запустите повторный анализ: проверка будет доступна сразу."
            : "Действие не требуется: активных задач на возврат для проверки нет.";
    }

    private static string BuildReportFileNameBase(Guid scanSessionId, DateTimeOffset generatedAtUtc)
    {
        var normalizedTimestamp = generatedAtUtc.ToUniversalTime().ToString("yyyyMMdd-HHmmss'Z'");
        return $"driverguardian-scan-report-{normalizedTimestamp}-{scanSessionId:N}";
    }
}

public sealed record MainScreenAssembledResult(
    IReadOnlyCollection<RecommendationDetailResult> RecommendationDetails,
    RecommendationDetailStats DetailStats,
    string VerificationSummary,
    ReportExportPayload ReportPayload,
    OpenOfficialSourceActionResult OfficialSourceAction);

public sealed record RecommendationDetailStats(int ManualHandoffReadyCount, int ManualActionRequiredCount);
