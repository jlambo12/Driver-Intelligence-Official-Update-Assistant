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
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class MainScreenWorkflowTests
{
    [Fact]
    public async Task RunScanAsync_ShouldUseDirectOfficialDriverPageResolution_ForOpenOfficialSourceAction()
    {
        var workflow = CreateWorkflow(new StaticRecommendationPipeline(BuildRecommendation(SourceTrustLevel.OfficialPublisherSite)));

        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Equal(1, result.ManualHandoffReadyCount);
        Assert.True(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionKind.DirectOfficialDriverPageConfirmed, result.OfficialSourceAction.Resolution);
    }

    [Fact]
    public async Task RunScanAsync_ShouldUseVendorSupportPageResolution_ForOpenOfficialSourceAction()
    {
        var workflow = CreateWorkflow(new StaticRecommendationPipeline(BuildRecommendation(SourceTrustLevel.OemSupportPortal)));

        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Equal(1, result.ManualHandoffReadyCount);
        Assert.True(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionKind.VendorSupportPageConfirmed, result.OfficialSourceAction.Resolution);
    }

    [Fact]
    public async Task RunScanAsync_ShouldExposeInsufficientEvidence_WhenSourceEvidenceMissing()
    {
        var recommendation = new RecommendationSummary(
            new DeviceIdentity("TEST\\DEVICE\\1"),
            true,
            "Stub recommendation",
            "2.0.0");

        var workflow = CreateWorkflow(new StaticRecommendationPipeline(recommendation));

        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Equal(0, result.ManualHandoffReadyCount);
        Assert.False(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionKind.InsufficientEvidence, result.OfficialSourceAction.Resolution);
        Assert.Equal(OpenOfficialSourceBlockedReason.SourceTrustUnverified.ToString(), result.OfficialSourceAction.BlockReason);
    }

    [Fact]
    public async Task RunScanAsync_ShouldFilterLowValueDevicesAndKeepUserRelevantResults()
    {
        var workflow = new MainScreenWorkflow(
            new MixedScanOrchestrator(),
            new NoRecommendationPipeline(),
            new FakeProviderCatalogSummaryService(),
            new FakeSettingsRepository(),
            new RecordingDiagnosticLogger(),
            new FakeAuditWriter(),
            new FakeHistoryRepository(),
            new OpenOfficialSourceActionEvaluator(),
            new ShareableReportBuilder());

        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Single(result.RecommendationDetails);
        Assert.Equal("Intel (Display)", result.RecommendationDetails.First().DeviceDisplayName);
        Assert.Equal("PCI\\VEN_8086&DEV_1234", result.RecommendationDetails.First().DeviceId);
    }

    private static RecommendationSummary BuildRecommendation(SourceTrustLevel trustLevel)
        => new(
            new DeviceIdentity("TEST\\DEVICE\\1"),
            true,
            "Stub recommendation",
            "2.0.0",
            providerCode: "test-provider",
            evidenceSourceUri: new Uri("https://vendor.example.com/drivers/device"),
            evidencePublisherName: "Vendor",
            evidenceTrustLevel: (int)trustLevel,
            evidenceIsOfficialSource: true,
            evidenceNote: "unit-test",
            officialSourceUri: new Uri("https://vendor.example.com/drivers/device/download"));

    private static MainScreenWorkflow CreateWorkflow(IRecommendationPipeline recommendationPipeline)
        => new(
            new FakeScanOrchestrator(),
            recommendationPipeline,
            new FakeProviderCatalogSummaryService(),
            new FakeSettingsRepository(),
            new RecordingDiagnosticLogger(),
            new FakeAuditWriter(),
            new FakeHistoryRepository(),
            new OpenOfficialSourceActionEvaluator(),
            new ShareableReportBuilder());

    private sealed class FakeScanOrchestrator : IScanOrchestrator
    {
        public Task<ScanResult> RunAsync(CancellationToken cancellationToken)
        {
            var session = ScanSession.Start(Guid.NewGuid(), DateTimeOffset.UtcNow).Complete(DateTimeOffset.UtcNow);
            IReadOnlyCollection<InstalledDriverSnapshot> drivers =
            [
                new InstalledDriverSnapshot(
                    new DeviceIdentity("TEST\\DEVICE\\1"),
                    new HardwareIdentifier("PCI\\VEN_1234&DEV_ABCD"),
                    "1.0.0",
                    null,
                    "Test")
            ];
            IReadOnlyCollection<DiscoveredDevice> discoveredDevices =
            [
                DiscoveredDevice.Create(
                    "TEST\\DEVICE\\1",
                    "Тестовое устройство",
                    ["PCI\\VEN_1234&DEV_ABCD"],
                    "Test",
                    "System",
                    DevicePresenceStatus.Present,
                    null)
            ];

            return Task.FromResult(new ScanResult(session, 2, discoveredDevices, drivers));
        }
    }

    private sealed class StaticRecommendationPipeline(RecommendationSummary recommendation) : IRecommendationPipeline
    {
        public Task<IReadOnlyCollection<RecommendationSummary>> BuildAsync(IReadOnlyCollection<InstalledDriverSnapshot> installedDrivers, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<RecommendationSummary>>([recommendation]);
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
                History = AppSettings.Default.History with { MaxEntries = 37 }
            });

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeAuditWriter : IAuditWriter
    {
        public List<string> Entries { get; } = [];

        public Task WriteAsync(string entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHistoryRepository : IResultHistoryRepository
    {
        public List<ResultHistoryEntry> Entries { get; } = [];

        public Task SaveAsync(ResultHistoryEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ResultHistoryEntry>> GetRecentAsync(int take, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ResultHistoryEntry>>(Entries.Take(take).ToArray());
    }

    private sealed class MixedScanOrchestrator : IScanOrchestrator
    {
        public Task<ScanResult> RunAsync(CancellationToken cancellationToken)
        {
            var session = ScanSession.Start(Guid.NewGuid(), DateTimeOffset.UtcNow).Complete(DateTimeOffset.UtcNow);
            IReadOnlyCollection<DiscoveredDevice> discoveredDevices =
            [
                DiscoveredDevice.Create(
                    "SWD\\MMDEVAPI\\{0.0.0.00000000}.{AAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}",
                    "SWD\\MMDEVAPI\\{0.0.0.00000000}.{AAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}",
                    ["ROOT\\MMDEVAPI"],
                    "Microsoft",
                    "AudioEndpoint",
                    DevicePresenceStatus.Present,
                    null),
                DiscoveredDevice.Create(
                    "PCI\\VEN_8086&DEV_1234",
                    "PCI\\VEN_8086&DEV_1234",
                    ["PCI\\VEN_8086&DEV_1234"],
                    "Intel",
                    "Display",
                    DevicePresenceStatus.Present,
                    null)
            ];

            IReadOnlyCollection<InstalledDriverSnapshot> drivers =
            [
                new InstalledDriverSnapshot(
                    new DeviceIdentity("SWD\\MMDEVAPI\\{0.0.0.00000000}.{AAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}"),
                    new HardwareIdentifier("ROOT\\MMDEVAPI"),
                    "10.0.1",
                    null,
                    "Microsoft"),
                new InstalledDriverSnapshot(
                    new DeviceIdentity("PCI\\VEN_8086&DEV_1234"),
                    new HardwareIdentifier("PCI\\VEN_8086&DEV_1234"),
                    "31.0.101.9999",
                    null,
                    "Intel")
            ];

            return Task.FromResult(new ScanResult(session, 2, discoveredDevices, drivers));
        }
    }

    private sealed class NoRecommendationPipeline : IRecommendationPipeline
    {
        public Task<IReadOnlyCollection<RecommendationSummary>> BuildAsync(IReadOnlyCollection<InstalledDriverSnapshot> installedDrivers, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<RecommendationSummary> result = installedDrivers
                .Select(driver => new RecommendationSummary(driver.DeviceIdentity, false, "Недостаточно данных", null))
                .ToArray();
            return Task.FromResult(result);
        }
    }

    private sealed class RecordingDiagnosticLogger : IDiagnosticLogger
    {
        public Task LogInfoAsync(string eventName, string message, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task LogErrorAsync(string eventName, string message, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
