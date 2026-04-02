using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.History;
using DriverGuardian.Application.History.Models;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Application.Reports;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Recommendations;
using DriverGuardian.Domain.Scanning;
using DriverGuardian.Domain.Settings;
using DriverGuardian.Contracts.DeviceDiscovery;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class MainScreenWorkflowTests
{
    [Fact]
    public async Task RunScanAsync_ShouldReturnUiReadyResult_WriteAudit_AndCaptureHistory()
    {
        var auditWriter = new FakeAuditWriter();
        var historyRepository = new FakeHistoryRepository();
        var workflow = new MainScreenWorkflow(
            new FakeScanOrchestrator(),
            new FakeRecommendationPipeline(),
            new FakeProviderCatalogSummaryService(),
            new FakeSettingsRepository(),
            new FakeDiagnosticLogger(),
            auditWriter,
            historyRepository,
            new OpenOfficialSourceActionEvaluator(),
            new ShareableReportBuilder());

        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Equal(2, result.DiscoveredDeviceCount);
        Assert.Equal(1, result.InspectedDriverCount);
        Assert.Equal(1, result.RecommendedCount);
        Assert.Equal(0, result.NotRecommendedCount);
        Assert.Equal(3, result.ProviderCount);
        Assert.Equal(0, result.ManualHandoffReadyCount);
        Assert.Equal(1, result.ManualHandoffUserActionCount);
        Assert.False(string.IsNullOrWhiteSpace(result.VerificationSummary));
        Assert.Equal("ru-RU", result.UiCulture);
        Assert.False(string.IsNullOrWhiteSpace(result.ReportExportPayload.FileNameBase));
        Assert.False(string.IsNullOrWhiteSpace(result.ReportExportPayload.PlainTextContent));
        Assert.Contains("DriverGuardian Scan Report", result.ReportExportPayload.MarkdownContent);
        Assert.Single(result.RecommendationDetails);
        Assert.False(result.OfficialSourceAction.IsReady);
        Assert.Equal(3, result.RecentHistory.Count);
        Assert.Equal(37, historyRepository.LastRequestedTake);
        Assert.Equal(3, historyRepository.Entries.Count);
        Assert.Contains(historyRepository.Entries, entry => entry is ScanHistoryEntry);
        Assert.Contains(historyRepository.Entries, entry => entry is RecommendationSummaryHistoryEntry);
        Assert.Contains(historyRepository.Entries, entry => entry is VerificationHistoryEntry);
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

    private sealed class FakeDiagnosticLogger : IDiagnosticLogger
    {
        public Task LogInfoAsync(string eventName, string message, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task LogErrorAsync(string eventName, string message, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;

        public string GetEffectiveLogDirectory() => Path.GetTempPath();

        public bool TryOpenEffectiveLogDirectory() => true;
    }

    private sealed class FakeHistoryRepository : IResultHistoryRepository
    {
        public List<ResultHistoryEntry> Entries { get; } = [];

        public int LastRequestedTake { get; private set; }

        public Task SaveAsync(ResultHistoryEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ResultHistoryEntry>> GetRecentAsync(int take, CancellationToken cancellationToken)
        {
            LastRequestedTake = take;
            return Task.FromResult<IReadOnlyCollection<ResultHistoryEntry>>(Entries.Take(take).ToArray());
        }
    }
}
