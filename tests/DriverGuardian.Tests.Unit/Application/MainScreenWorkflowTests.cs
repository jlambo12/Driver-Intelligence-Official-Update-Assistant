using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Recommendations;
using DriverGuardian.Domain.Scanning;
using DriverGuardian.Domain.Settings;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class MainScreenWorkflowTests
{
    [Fact]
    public async Task RunScanAsync_ShouldReturnUiReadyResult_AndWriteAudit()
    {
        var auditWriter = new FakeAuditWriter();
        var workflow = new MainScreenWorkflow(
            new FakeScanOrchestrator(),
            new FakeRecommendationPipeline(),
            new FakeProviderCatalogSummaryService(),
            new FakeSettingsRepository(),
            auditWriter);

        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Equal(1, result.DriverCount);
        Assert.Equal(1, result.RecommendationCount);
        Assert.Equal(3, result.ProviderCount);
        Assert.Equal("ru-RU", result.UiCulture);
        Assert.Single(auditWriter.Entries);
    }

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

            return Task.FromResult(new ScanResult(session, drivers));
        }
    }

    private sealed class FakeRecommendationPipeline : IRecommendationPipeline
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
        public Task<AppSettings> GetAsync(CancellationToken cancellationToken) => Task.FromResult(new AppSettings(true, "ru-RU"));

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
}
