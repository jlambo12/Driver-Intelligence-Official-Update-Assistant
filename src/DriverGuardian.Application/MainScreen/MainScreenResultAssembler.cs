using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Reports;
using DriverGuardian.Application.Verification;
using DriverGuardian.Domain.Recommendations;
using DriverGuardian.Domain.Scanning;

namespace DriverGuardian.Application.MainScreen;

public sealed class MainScreenResultAssembler(
    RecommendationDetailAssembler recommendationDetailAssembler,
    OfficialSourceActionService officialSourceActionService,
    VerificationTrackingService verificationTrackingService,
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
        var verifications = await verificationTrackingService.EvaluateAndCaptureAsync(scanResult.Drivers, recommendations, cancellationToken);
        var verificationSummary = BuildVerificationSummary(verifications, recommendationDetails);

        var generatedAtUtc = DateTimeOffset.UtcNow;
        var report = reportBuilder.Build(
            new ShareableReportRequest(
                scanResult,
                recommendations,
                [],
                verifications,
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

    private static string BuildVerificationSummary(
        IReadOnlyCollection<VerificationReportItem> verifications,
        IReadOnlyCollection<RecommendationDetailResult> recommendationDetails)
    {
        if (verifications.Count == 0)
        {
            var waitingForReturnCount = recommendationDetails.Count(detail => detail.VerificationAvailable);
            return waitingForReturnCount > 0
                ? $"Ожидается возврат пользователя по {waitingForReturnCount} устройств(ам). После ручной установки вернитесь и запустите повторный анализ: проверка будет доступна сразу."
                : "Действие не требуется: активных задач на возврат для проверки нет.";
        }

        var verified = verifications.Count(v => v.Result.Outcome == PostInstallVerificationOutcome.VerifiedChanged);
        var partial = verifications.Count(v => v.Result.Outcome == PostInstallVerificationOutcome.PartiallyChanged);
        var noChange = verifications.Count(v => v.Result.Outcome == PostInstallVerificationOutcome.NoChangeDetected);
        var missing = verifications.Count(v => v.Result.Outcome == PostInstallVerificationOutcome.DeviceMissing);
        var insufficient = verifications.Count(v => v.Result.Outcome == PostInstallVerificationOutcome.InsufficientEvidence);

        return $"Результаты проверки после ручной установки: подтверждено изменений {verified}, частично {partial}, без изменений {noChange}, устройство отсутствует {missing}, недостаточно данных {insufficient}.";
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
