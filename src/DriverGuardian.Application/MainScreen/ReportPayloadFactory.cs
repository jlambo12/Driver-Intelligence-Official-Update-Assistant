using DriverGuardian.Application.Reports;
using DriverGuardian.Domain.Recommendations;
using DriverGuardian.Domain.Scanning;

namespace DriverGuardian.Application.MainScreen;

public sealed class ReportPayloadFactory(IShareableReportBuilder reportBuilder)
{
    public ReportExportPayload Create(
        ScanResult scanResult,
        IReadOnlyCollection<RecommendationSummary> recommendations)
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
        var markdownContent = $"# DriverGuardian Shareable Scan Report{Environment.NewLine}{Environment.NewLine}```text{Environment.NewLine}{plainTextContent}{Environment.NewLine}```";
        var fileNameBase = BuildReportFileNameBase(scanResult.Session.Id, generatedAtUtc);

        return new ReportExportPayload(fileNameBase, plainTextContent, markdownContent);
    }

    private static string BuildReportFileNameBase(Guid scanSessionId, DateTimeOffset generatedAtUtc)
    {
        var normalizedTimestamp = generatedAtUtc.ToUniversalTime().ToString("yyyyMMdd-HHmmss'Z'");
        return $"driverguardian-scan-report-{normalizedTimestamp}-{scanSessionId:N}";
    }
}
