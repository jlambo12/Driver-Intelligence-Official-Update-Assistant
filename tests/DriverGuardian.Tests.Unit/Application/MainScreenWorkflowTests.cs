using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.History;
using DriverGuardian.Application.History.Models;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Application.Reports;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Recommendations;
using DriverGuardian.Domain.Scanning;
using DriverGuardian.Domain.Settings;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class MainScreenWorkflowTests
{
    [Fact]
    public async Task RunScanAsync_ShouldReturnDirectOfficialResolution_WhenDirectEvidenceExists()
    {
        var workflow = CreateWorkflow(new StaticRecommendationPipeline([
            BuildRecommendation("TEST\\DEVICE\\1", RecommendationSourceTrustLevel.OfficialPublisherSite, "https://vendor.test/drivers/direct")
        ]));

        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.True(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionKind.DirectOfficialDriverPageConfirmed, result.OfficialSourceAction.Resolution);
        Assert.Equal(1, result.ManualHandoffReadyCount);
    }

    [Fact]
    public async Task RunScanAsync_ShouldReturnVendorSupportResolution_WhenDirectEvidenceDoesNotExist()
    {
        var workflow = CreateWorkflow(new StaticRecommendationPipeline([
            BuildRecommendation("TEST\\DEVICE\\1", RecommendationSourceTrustLevel.OemSupportPortal, "https://vendor.test/support/device")
        ]));

        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.True(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionKind.VendorSupportPageConfirmed, result.OfficialSourceAction.Resolution);
    }

    [Fact]
    public async Task RunScanAsync_ShouldReturnInsufficientEvidence_WhenNoConfirmedSourceExists()
    {
        var recommendation = new RecommendationSummary(
            new DeviceIdentity("TEST\\DEVICE\\1"),
            hasRecommendation: true,
            reason: "stub",
            recommendedVersion: "2.0.0");

        var workflow = CreateWorkflow(new StaticRecommendationPipeline([recommendation]));

        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.False(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionKind.InsufficientEvidence, result.OfficialSourceAction.Resolution);
    }

    [Fact]
    public async Task RunScanAsync_ShouldChooseBestActionableConfirmedSource_WhenMultipleRecommendationsExist()
    {
        var workflow = CreateWorkflow(new StaticRecommendationPipeline([
            BuildRecommendation("TEST\\DEVICE\\1", RecommendationSourceTrustLevel.OemSupportPortal, "https://vendor.test/support/device"),
            BuildRecommendation("TEST\\DEVICE\\2", RecommendationSourceTrustLevel.OfficialPublisherSite, "https://vendor.test/drivers/direct")
        ]));

        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.True(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionKind.DirectOfficialDriverPageConfirmed, result.OfficialSourceAction.Resolution);
        Assert.Equal("https://vendor.test/drivers/direct", result.OfficialSourceAction.ApprovedOfficialSourceUrl);
    }

    private static MainScreenWorkflow CreateWorkflow(IRecommendationPipeline recommendationPipeline)
        => new(
            new FakeScanOrchestrator(),
            recommendationPipeline,
            new FakeProviderCatalogSummaryService(),
            new FakeSettingsRepository(),
            new NoOpDiagnosticLogger(),
            new FakeAuditWriter(),
            new FakeHistoryRepository(),
            new OpenOfficialSourceActionEvaluator(),
            new ShareableReportBuilder());

    private static RecommendationSummary BuildRecommendation(string deviceId, RecommendationSourceTrustLevel trustLevel, string officialSourceUrl)
        => new(
            new DeviceIdentity(deviceId),
            hasRecommendation: true,
            reason: "stub",
            recommendedVersion: "2.0.0",
            sourceEvidence: new RecommendationSourceEvidence(
                "provider-test",
                new Uri(officialSourceUrl),
                "Vendor",
                trustLevel,
                true,
                "unit-test",
                new Uri(officialSourceUrl)));

    private sealed class FakeScanOrchestrator : IScanOrchestrator
    {
        public Task<ScanResult> RunAsync(CancellationToken cancellationToken)
        {
            var session = ScanSession.Start(Guid.NewGuid(), DateTimeOffset.UtcNow).Complete(DateTimeOffset.UtcNow);
            IReadOnlyCollection<InstalledDriverSnapshot> drivers =
            [
                new(new DeviceIdentity("TEST\\DEVICE\\1"), new HardwareIdentifier("PCI\\VEN_1111&DEV_0001"), "1.0.0", null, "Vendor"),
                new(new DeviceIdentity("TEST\\DEVICE\\2"), new HardwareIdentifier("PCI\\VEN_1111&DEV_0002"), "1.0.0", null, "Vendor")
            ];

            IReadOnlyCollection<DiscoveredDevice> discoveredDevices =
            [
                DiscoveredDevice.Create("TEST\\DEVICE\\1", "Device One", ["PCI\\VEN_1111&DEV_0001"], "Vendor", "System", DevicePresenceStatus.Present, null),
                DiscoveredDevice.Create("TEST\\DEVICE\\2", "Device Two", ["PCI\\VEN_1111&DEV_0002"], "Vendor", "System", DevicePresenceStatus.Present, null)
            ];

            return Task.FromResult(new ScanResult(session, 2, discoveredDevices, drivers));
        }
    }

    private sealed class StaticRecommendationPipeline(IReadOnlyCollection<RecommendationSummary> recommendations) : IRecommendationPipeline
    {
        public Task<IReadOnlyCollection<RecommendationSummary>> BuildAsync(IReadOnlyCollection<InstalledDriverSnapshot> installedDrivers, CancellationToken cancellationToken)
            => Task.FromResult(recommendations);
    }

    private sealed class FakeProviderCatalogSummaryService : IProviderCatalogSummaryService
    {
        public Task<int> GetProviderCountAsync(CancellationToken cancellationToken) => Task.FromResult(3);
    }

    private sealed class FakeSettingsRepository : ISettingsRepository
    {
        public Task<AppSettings> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult(AppSettings.Default with
            {
                Localization = new LocalizationPreferences("ru-RU"),
                History = AppSettings.Default.History with { MaxEntries = 25 }
            });

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeAuditWriter : IAuditWriter
    {
        public Task WriteAsync(string entry, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeHistoryRepository : IResultHistoryRepository
    {
        public Task SaveAsync(ResultHistoryEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyCollection<ResultHistoryEntry>> GetRecentAsync(int take, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ResultHistoryEntry>>([]);
    }

    private sealed class NoOpDiagnosticLogger : IDiagnosticLogger
    {
        public Task LogInfoAsync(string eventName, string message, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task LogErrorAsync(string eventName, string message, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
