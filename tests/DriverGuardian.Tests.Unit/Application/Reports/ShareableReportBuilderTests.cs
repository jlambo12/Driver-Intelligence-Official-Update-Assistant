using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Downloads;
using DriverGuardian.Application.Reports;
using DriverGuardian.Application.Verification;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Recommendations;
using DriverGuardian.Domain.Scanning;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Tests.Unit.Application.Reports;

public sealed class ShareableReportBuilderTests
{
    private readonly ShareableReportBuilder _builder = new();

    [Fact]
    public void Build_CreatesExpectedSummarySectionsAndDeviceDetails()
    {
        var request = BuildRequest();

        var report = _builder.Build(request);

        Assert.Equal(2, report.ScanSummary.TotalDevices);
        Assert.Equal(2, report.RecommendationSummary.TotalRecommendations);
        Assert.Equal(1, report.RecommendationSummary.RecommendedCount);
        Assert.Equal(2, report.ManualInstallHandoffSummary.TotalHandoffs);
        Assert.Equal(1, report.ManualInstallHandoffSummary.ReadyCount);
        Assert.Equal(1, report.ManualInstallHandoffSummary.RequiresActionCount);
        Assert.Equal(2, report.VerificationSummary.TotalVerifications);
        Assert.Equal(1, report.VerificationSummary.VerifiedChangedCount);
        Assert.Equal(1, report.VerificationSummary.NoChangeCount);

        var device = Assert.Single(report.Devices.Where(x => x.DeviceInstanceId == "PCI\\VEN_1111"));
        Assert.NotNull(device.Recommendation);
        Assert.NotNull(device.ManualInstallHandoff);
        Assert.NotNull(device.Verification);
        Assert.Equal("2.0.0", device.Recommendation!.RecommendedVersion);
    }

    [Fact]
    public void BuildStructuredText_ContainsTopLevelSections()
    {
        var report = _builder.Build(BuildRequest());

        var text = _builder.BuildStructuredText(report);

        Assert.Contains("DriverGuardian Scan Report", text);
        Assert.Contains("Scan Summary", text);
        Assert.Contains("Recommendation Summary", text);
        Assert.Contains("Manual Install Handoff Summary", text);
        Assert.Contains("Verification Summary", text);
        Assert.Contains("Device Details", text);
    }

    private static ShareableReportRequest BuildRequest()
    {
        var now = DateTimeOffset.Parse("2026-04-01T12:00:00+00:00");
        var session = ScanSession.Start(Guid.NewGuid(), now.AddMinutes(-5)).Complete(now.AddMinutes(-2));

        var driver1 = BuildDriver("PCI\\VEN_1111", "1.0.0", "VendorA");
        var driver2 = BuildDriver("PCI\\VEN_2222", "1.5.0", "VendorB");

        return new ShareableReportRequest(
            new ScanResult(session, [driver1, driver2]),
            [
                new RecommendationSummary(new DeviceIdentity("PCI\\VEN_1111"), true, "Compatible upgrade available", "2.0.0"),
                new RecommendationSummary(new DeviceIdentity("PCI\\VEN_2222"), false, "Already up to date", null)
            ],
            [
                new ManualInstallHandoffReportItem(new DeviceIdentity("PCI\\VEN_1111"), BuildReadyHandoff()),
                new ManualInstallHandoffReportItem(new DeviceIdentity("PCI\\VEN_2222"), BuildBlockedHandoff())
            ],
            [
                new VerificationReportItem(new DeviceIdentity("PCI\\VEN_1111"), BuildVerificationResult(PostInstallVerificationOutcome.VerifiedChanged)),
                new VerificationReportItem(new DeviceIdentity("PCI\\VEN_2222"), BuildVerificationResult(PostInstallVerificationOutcome.NoChangeDetected))
            ],
            now);
    }

    private static InstalledDriverSnapshot BuildDriver(string instanceId, string version, string provider)
        => new(
            new DeviceIdentity(instanceId),
            new HardwareIdentifier($"HWID-{instanceId}"),
            version,
            new DateOnly(2026, 3, 31),
            provider);

    private static ManualInstallHandoffDecision BuildReadyHandoff()
        => new(
            HandoffReadinessOutcome.ReadyForManualInstallHandoff,
            new OfficialPackageReference(
                "official",
                "DRV-1",
                "2.0.0",
                new Uri("https://vendor.example/driver.cab"),
                new SourceEvidence(
                    new Uri("https://vendor.example/catalog"),
                    "Vendor",
                    SourceTrustLevel.OfficialPublisherSite,
                    true,
                    "Trusted source")),
            []);

    private static ManualInstallHandoffDecision BuildBlockedHandoff()
        => new(
            HandoffReadinessOutcome.UserActionRequired,
            null,
            [new UserActionRequiredReason(HandoffBlockReason.PackageUrlIsNotHttps, "Package URL must use HTTPS")]);

    private static PostInstallVerificationResult BuildVerificationResult(PostInstallVerificationOutcome outcome)
        => new(
            outcome,
            PostInstallVerificationReason.None,
            new DriverStateComparisonResult([], outcome == PostInstallVerificationOutcome.VerifiedChanged, false, false),
            "verification");
}
