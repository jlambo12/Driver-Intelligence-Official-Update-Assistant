using DriverGuardian.Application.Presentation;
using DriverGuardian.Application.Verification;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Recommendations;

namespace DriverGuardian.Application.Reports;

internal sealed class ShareableReportModelAssembler
{
    public ShareableReport Build(ShareableReportRequest request)
    {
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

    private static DeviceReportSection BuildDeviceSection(
        InstalledDriverSnapshot driver,
        Contracts.DeviceDiscovery.DiscoveredDevice? discoveredDevice,
        RecommendationSummary? recommendation,
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
                    MapOfficialSourceConfidence(handoff),
                    BuildOfficialSourceGuidance(handoff),
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

    private static string MapOfficialSourceConfidence(ManualInstallHandoffReportItem handoff)
    {
        var evidence = handoff.Decision.PackageReference?.SourceEvidence;
        if (evidence is null)
        {
            return "Unconfirmed";
        }

        if (evidence.IsOfficialSource &&
            evidence.TrustLevel == ProviderAdapters.Abstractions.Lookup.SourceTrustLevel.OfficialPublisherSite)
        {
            return "Confirmed official publisher source";
        }

        return evidence.TrustLevel switch
        {
            ProviderAdapters.Abstractions.Lookup.SourceTrustLevel.OfficialPublisherSite => "Likely official source (not confirmed)",
            ProviderAdapters.Abstractions.Lookup.SourceTrustLevel.OemSupportPortal => "Unconfirmed third-party source",
            ProviderAdapters.Abstractions.Lookup.SourceTrustLevel.OperatingSystemCatalog => "Unconfirmed third-party source",
            _ => "Unconfirmed"
        };
    }

    private static string BuildOfficialSourceGuidance(ManualInstallHandoffReportItem handoff)
    {
        var evidence = handoff.Decision.PackageReference?.SourceEvidence;
        if (evidence?.IsOfficialSource == true)
        {
            return "Review the vendor page and complete installation manually.";
        }

        return "Do not treat this link as confirmed official. Verify publisher ownership manually before downloading.";
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
