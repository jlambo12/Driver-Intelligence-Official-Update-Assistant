using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Downloads;
using DriverGuardian.Application.Reports;
using DriverGuardian.Application.Verification;
using DriverGuardian.Contracts.DeviceDiscovery;
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

        Assert.Contains("DriverGuardian Shareable Scan Report", text);
        Assert.Contains("1) Scan Summary", text);
        Assert.Contains("2) Recommendation State", text);
        Assert.Contains("3) Manual Next-Step Guidance", text);
        Assert.Contains("4) Verification State", text);
        Assert.Contains("5) Device-Level Details", text);
        Assert.Contains("Installation is always manual and user-controlled", text);
    }

    [Fact]
    public void Build_ShouldFilterLowValueTechnicalDevicesFromUserFacingReport()
    {
        var now = DateTimeOffset.Parse("2026-04-01T12:00:00+00:00");
        var session = ScanSession.Start(Guid.NewGuid(), now.AddMinutes(-5)).Complete(now.AddMinutes(-1));

        var swdDriver = BuildDriver("SWD\\MMDEVAPI\\{FAKE}", "10.0.1", "Microsoft");
        var gpuDriver = BuildDriver("PCI\\VEN_10DE&DEV_1C8D", "552.44", "NVIDIA");
        var request = new ShareableReportRequest(
            new ScanResult(
                session,
                2,
                [
                    DiscoveredDevice.Create("SWD\\MMDEVAPI\\{FAKE}", "SWD\\MMDEVAPI\\{FAKE}", ["ROOT\\MMDEVAPI"], "Microsoft", "AudioEndpoint", DevicePresenceStatus.Present, null),
                    DiscoveredDevice.Create("PCI\\VEN_10DE&DEV_1C8D", "PCI\\VEN_10DE&DEV_1C8D", ["PCI\\VEN_10DE&DEV_1C8D"], "NVIDIA", "Display", DevicePresenceStatus.Present, null)
                ],
                [swdDriver, gpuDriver]),
            [
                new RecommendationSummary(swdDriver.DeviceIdentity, false, "No action", null),
                new RecommendationSummary(gpuDriver.DeviceIdentity, false, "No action", null)
            ],
            [],
            [],
            now);

        var report = _builder.Build(request);

        var onlyDevice = Assert.Single(report.Devices);
        Assert.Equal("PCI\\VEN_10DE&DEV_1C8D", onlyDevice.DeviceInstanceId);
        Assert.Equal("NVIDIA (Display)", onlyDevice.DeviceDisplayName);
    }

    [Fact]
    public void BuildStructuredText_ShouldUseHonestOfficialSourceLanguage_WhenSourceIsNotConfirmed()
    {
        var request = BuildRequest();
        var report = _builder.Build(request);

        var text = _builder.BuildStructuredText(report);

        Assert.Contains("Official Source Confidence: Unconfirmed", text);
        Assert.Contains("Do not treat this link as confirmed official", text);
    }

    [Fact]
    public void BuildStructuredText_ShouldShowConfirmedOfficialSource_WhenEvidenceIsConfirmed()
    {
        var now = DateTimeOffset.Parse("2026-04-01T12:00:00+00:00");
        var session = ScanSession.Start(Guid.NewGuid(), now.AddMinutes(-5)).Complete(now.AddMinutes(-2));
        var driver = BuildDriver("PCI\\VEN_9999", "4.2.0", "VendorC");
        var handoff = new ManualInstallHandoffDecision(
            HandoffReadinessOutcome.ReadyForManualInstallHandoff,
            new OfficialPackageReference(
                "official",
                "DRV-9",
                "4.3.0",
                new Uri("https://vendorc.example/driver.cab"),
                new SourceEvidence(
                    new Uri("https://vendorc.example/support"),
                    "VendorC",
                    SourceTrustLevel.OfficialPublisherSite,
                    true,
                    "Publisher-owned domain confirmed")),
            []);

        var request = new ShareableReportRequest(
            new ScanResult(session, 1, BuildDiscoveredDevices(driver), [driver]),
            [new RecommendationSummary(driver.DeviceIdentity, true, "Upgrade available", "4.3.0")],
            [new ManualInstallHandoffReportItem(driver.DeviceIdentity, handoff)],
            [],
            now);

        var text = _builder.BuildStructuredText(_builder.Build(request));

        Assert.Contains("Official Source Confidence: Confirmed official publisher source", text);
        Assert.Contains("Review the vendor page and complete installation manually", text);
    }

    private static ShareableReportRequest BuildRequest()
    {
        var now = DateTimeOffset.Parse("2026-04-01T12:00:00+00:00");
        var session = ScanSession.Start(Guid.NewGuid(), now.AddMinutes(-5)).Complete(now.AddMinutes(-2));

        var driver1 = BuildDriver("PCI\\VEN_1111", "1.0.0", "VendorA");
        var driver2 = BuildDriver("PCI\\VEN_2222", "1.5.0", "VendorB");

        return new ShareableReportRequest(
            new ScanResult(session, 2, BuildDiscoveredDevices(driver1, driver2), [driver1, driver2]),
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

    private static IReadOnlyCollection<DiscoveredDevice> BuildDiscoveredDevices(params InstalledDriverSnapshot[] drivers)
        => drivers
            .Select(driver => DiscoveredDevice.Create(
                driver.DeviceIdentity.InstanceId,
                driver.DeviceIdentity.InstanceId,
                [driver.HardwareIdentifier.Value],
                driver.ProviderName,
                null,
                DevicePresenceStatus.Present,
                null))
            .ToArray();

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
