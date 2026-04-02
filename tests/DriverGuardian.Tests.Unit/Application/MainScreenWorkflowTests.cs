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
    public async Task RunScanAsync_ShouldUseDirectOfficialDriverPageOutcome_WhenEvidenceIsOfficialPublisher()
    {
        var result = await CreateWorkflow(new FakeRecommendationPipeline(SourceTrustLevel.OfficialPublisherSite)).RunScanAsync(CancellationToken.None);

        Assert.True(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionOutcome.DirectOfficialDriverPageConfirmed, result.OfficialSourceAction.ResolutionOutcome);
        Assert.Equal(1, result.ManualHandoffReadyCount);
    }

    [Fact]
    public async Task RunScanAsync_ShouldUseVendorSupportOutcome_WhenEvidenceIsOemSupport()
    {
        var result = await CreateWorkflow(new FakeRecommendationPipeline(SourceTrustLevel.OemSupportPortal)).RunScanAsync(CancellationToken.None);

        Assert.True(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionOutcome.VendorSupportPageConfirmed, result.OfficialSourceAction.ResolutionOutcome);
        Assert.Equal(1, result.ManualHandoffReadyCount);
    }

    [Fact]
    public async Task RunScanAsync_ShouldRemainBlocked_WhenOfficialSourceEvidenceIsInsufficient()
    {
        var result = await CreateWorkflow(new InsufficientEvidenceRecommendationPipeline()).RunScanAsync(CancellationToken.None);

        Assert.False(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionOutcome.InsufficientEvidence, result.OfficialSourceAction.ResolutionOutcome);
        Assert.Equal("ResolutionNotConfirmed", result.OfficialSourceAction.BlockReason);
        Assert.Equal(0, result.ManualHandoffReadyCount);
    }

    private static MainScreenWorkflow CreateWorkflow(IRecommendationPipeline pipeline)
        => new(
            new FakeScanOrchestrator(),
            pipeline,
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
                new InstalledDriverSnapshot(new DeviceIdentity("TEST\\DEVICE\\1"), new HardwareIdentifier("PCI\\VEN_1234&DEV_ABCD"), "1.0.0", null, "Test")
            ];
            IReadOnlyCollection<DiscoveredDevice> discoveredDevices =
            [
                DiscoveredDevice.Create("TEST\\DEVICE\\1", "Тестовое устройство", ["PCI\\VEN_1234&DEV_ABCD"], "Test", "System", DevicePresenceStatus.Present, null)
            ];

            return Task.FromResult(new ScanResult(session, 1, discoveredDevices, drivers));
        }
    }

    private sealed class FakeRecommendationPipeline(SourceTrustLevel trustLevel) : IRecommendationPipeline
    {
        public Task<IReadOnlyCollection<RecommendationSummary>> BuildAsync(IReadOnlyCollection<InstalledDriverSnapshot> installedDrivers, CancellationToken cancellationToken)
        {
            var evidence = new SourceEvidence(
                SourceUri: new Uri("https://downloads.vendor.test/provider"),
                PublisherName: "Vendor",
                TrustLevel: trustLevel,
                IsOfficialSource: true,
                EvidenceNote: "test");

            IReadOnlyCollection<RecommendationSummary> result =
            [
                new RecommendationSummary(installedDrivers.First().DeviceIdentity, true, "Stub recommendation", "2.0.0", "official", evidence, new Uri("https://downloads.vendor.test/catalog/driver"))
            ];

            return Task.FromResult(result);
        }
    }

    private sealed class InsufficientEvidenceRecommendationPipeline : IRecommendationPipeline
    {
        public Task<IReadOnlyCollection<RecommendationSummary>> BuildAsync(IReadOnlyCollection<InstalledDriverSnapshot> installedDrivers, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<RecommendationSummary> result =
            [
                new RecommendationSummary(installedDrivers.First().DeviceIdentity, true, "Stub recommendation", "2.0.0")
            ];

            return Task.FromResult(result);
        }
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
        public Task WriteAsync(string entry, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeHistoryRepository : IResultHistoryRepository
    {
        public Task SaveAsync(ResultHistoryEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyCollection<ResultHistoryEntry>> GetRecentAsync(int take, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ResultHistoryEntry>>(Array.Empty<ResultHistoryEntry>());
    }
}
