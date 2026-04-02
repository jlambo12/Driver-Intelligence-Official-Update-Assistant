using DriverGuardian.Application.Verification;
using DriverGuardian.Application.Presentation;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.Application.Reports;

public interface IShareableReportBuilder
{
    ShareableReport Build(ShareableReportRequest request);
    string BuildStructuredText(ShareableReport report);
}

public sealed class ShareableReportBuilder : IShareableReportBuilder
{
    public ShareableReport Build(ShareableReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var recommendationByDevice = request.Recommendations.ToDictionary(x => x.DeviceIdentity.InstanceId, StringComparer.OrdinalIgnoreCase);
        var handoffByDevice = request.ManualInstallHandoffs.ToDictionary(x => x.DeviceIdentity.InstanceId, StringComparer.OrdinalIgnoreCase);
        var verificationByDevice = request.Verifications.ToDictionary(x => x.DeviceIdentity.InstanceId, StringComparer.OrdinalIgnoreCase);
        var discoveredByInstanceId = request.ScanResult.DiscoveredDevices.ToDictionary(
            x => x.Identity.InstanceId,
            x => x,
            StringComparer.OrdinalIgnoreCase);

        var devices = request.ScanResult.Drivers
            .Select(driver =>
            {
                discoveredByInstanceId.TryGetValue(driver.DeviceIdentity.InstanceId, out var discoveredDevice);
                recommendationByDevice.TryGetValue(driver.DeviceIdentity.InstanceId, out var recommendation);
                var hasRecommendation = recommendation?.HasRecommendation ?? false;
                return new
                {
                    Section = BuildDeviceSection(driver, discoveredDevice, recommendation, handoffByDevice, verificationByDevice),
                    Priority = DevicePresentationHeuristics.ResolvePriorityBucket(discoveredDevice, hasRecommendation),
                    IsRelevant = DevicePresentationHeuristics.IsUserRelevant(discoveredDevice, hasRecommendation)
                };
            })
            .Where(x => x.IsRelevant || x.Section.Recommendation?.HasRecommendation == true)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Section.DeviceDisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => x.Section)
            .ToArray();

        return new ShareableReport(
            new ReportMetadata(
                request.ScanResult.Session.Id,
                request.ScanResult.Session.StartedAtUtc,
                request.ScanResult.Session.CompletedAtUtc,
                request.GeneratedAtUtc),
            new ScanSummarySection(devices.Length),
            BuildRecommendationSummary(request),
            BuildHandoffSummary(request),
            BuildVerificationSummary(request),
            devices);
    }

    public string BuildStructuredText(ShareableReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var lines = new List<string>
        {
            "DriverGuardian Scan Report",
            $"Scan Session: {report.Metadata.ScanSessionId}",
            $"Scan Started (UTC): {report.Metadata.ScanStartedAtUtc:O}",
            $"Scan Completed (UTC): {report.Metadata.ScanCompletedAtUtc:O}",
            $"Generated (UTC): {report.Metadata.GeneratedAtUtc:O}",
            string.Empty,
            "Scan Summary",
            $"- Total Devices: {report.ScanSummary.TotalDevices}",
            string.Empty,
            "Recommendation Summary",
            $"- Total: {report.RecommendationSummary.TotalRecommendations}",
            $"- Recommended: {report.RecommendationSummary.RecommendedCount}",
            $"- Not Recommended: {report.RecommendationSummary.NotRecommendedCount}",
            string.Empty,
            "Manual Install Handoff Summary",
            $"- Total: {report.ManualInstallHandoffSummary.TotalHandoffs}",
            $"- Ready: {report.ManualInstallHandoffSummary.ReadyCount}",
            $"- Requires Action: {report.ManualInstallHandoffSummary.RequiresActionCount}",
            $"- Not Ready: {report.ManualInstallHandoffSummary.NotReadyCount}",
            string.Empty,
            "Verification Summary",
            $"- Total: {report.VerificationSummary.TotalVerifications}",
            $"- Verified Changed: {report.VerificationSummary.VerifiedChangedCount}",
            $"- Partial: {report.VerificationSummary.PartialCount}",
            $"- No Change: {report.VerificationSummary.NoChangeCount}",
            $"- Device Missing: {report.VerificationSummary.DeviceMissingCount}",
            $"- Insufficient Evidence: {report.VerificationSummary.InsufficientEvidenceCount}",
            string.Empty,
            "Device Details"
        };

        foreach (var device in report.Devices)
        {
            lines.Add($"- Device: {device.DeviceDisplayName}");
            lines.Add($"  Device ID: {device.DeviceInstanceId}");
            lines.Add($"  Driver Version: {device.DriverSnapshot.DriverVersion}");
            lines.Add($"  Provider: {device.DriverSnapshot.ProviderName ?? "(unknown)"}");
            lines.Add($"  Hardware ID: {device.DriverSnapshot.HardwareId}");

            if (device.Recommendation is not null)
            {
                lines.Add($"  Recommendation: {(device.Recommendation.HasRecommendation ? "Yes" : "No")}");
                lines.Add($"  Recommendation Reason: {device.Recommendation.Reason}");
            }

            if (device.ManualInstallHandoff is not null)
            {
                lines.Add($"  Handoff Outcome: {device.ManualInstallHandoff.Outcome}");
            }

            if (device.Verification is not null)
            {
                lines.Add($"  Verification Outcome: {device.Verification.Outcome}");
            }

            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static DeviceReportSection BuildDeviceSection(
        InstalledDriverSnapshot driver,
        Contracts.DeviceDiscovery.DiscoveredDevice? discoveredDevice,
        Domain.Recommendations.RecommendationSummary? recommendation,
        IReadOnlyDictionary<string, ManualInstallHandoffReportItem> handoffByDevice,
        IReadOnlyDictionary<string, VerificationReportItem> verificationByDevice)
    {
        handoffByDevice.TryGetValue(driver.DeviceIdentity.InstanceId, out var handoff);
        verificationByDevice.TryGetValue(driver.DeviceIdentity.InstanceId, out var verification);

        return new DeviceReportSection(
            DevicePresentationHeuristics.BuildUserFacingName(discoveredDevice, driver.DeviceIdentity.InstanceId),
            driver.DeviceIdentity.InstanceId,
            new ScanDriverSnapshotSection(
                driver.DriverVersion,
                driver.DriverDate?.ToString("yyyy-MM-dd"),
                driver.ProviderName,
                driver.HardwareIdentifier.Value),
            recommendation is null
                ? null
                : new RecommendationReportSection(
                    recommendation.HasRecommendation,
                    recommendation.Reason,
                    recommendation.RecommendedVersion),
            handoff is null
                ? null
                : new ManualInstallHandoffReportSection(
                    handoff.Decision.Outcome.ToString(),
                    handoff.Decision.IsHandoffReady,
                    handoff.Decision.PackageReference?.PackageUri.ToString(),
                    handoff.Decision.Reasons.Select(reason => reason.Message).ToArray()),
            verification is null
                ? null
                : new VerificationReportSection(
                    verification.Result.Outcome.ToString(),
                    verification.Result.Reason.ToString(),
                    verification.Result.IsVerifiedChanged,
                    verification.Result.Message,
                    verification.Result.Comparison?.Differences.Select(difference => difference.Description).ToArray() ?? []));
    }

    private static RecommendationSummarySection BuildRecommendationSummary(ShareableReportRequest request)
    {
        var recommendedCount = request.Recommendations.Count(x => x.HasRecommendation);
        return new RecommendationSummarySection(
            request.Recommendations.Count,
            recommendedCount,
            request.Recommendations.Count - recommendedCount);
    }

    private static ManualInstallHandoffSummarySection BuildHandoffSummary(ShareableReportRequest request)
    {
        var ready = request.ManualInstallHandoffs.Count(x => x.Decision.IsHandoffReady);
        var requiresAction = request.ManualInstallHandoffs.Count(x => x.Decision.Outcome == Downloads.HandoffReadinessOutcome.UserActionRequired);
        return new ManualInstallHandoffSummarySection(
            request.ManualInstallHandoffs.Count,
            ready,
            requiresAction,
            request.ManualInstallHandoffs.Count - ready);
    }

    private static VerificationSummarySection BuildVerificationSummary(ShareableReportRequest request)
    {
        return new VerificationSummarySection(
            request.Verifications.Count,
            request.Verifications.Count(x => x.Result.Outcome == PostInstallVerificationOutcome.VerifiedChanged),
            request.Verifications.Count(x => x.Result.Outcome == PostInstallVerificationOutcome.PartiallyChanged),
            request.Verifications.Count(x => x.Result.Outcome == PostInstallVerificationOutcome.NoChangeDetected),
            request.Verifications.Count(x => x.Result.Outcome == PostInstallVerificationOutcome.DeviceMissing),
            request.Verifications.Count(x => x.Result.Outcome == PostInstallVerificationOutcome.InsufficientEvidence));
    }
}
