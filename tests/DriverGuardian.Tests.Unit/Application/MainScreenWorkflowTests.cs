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
using System.Text.RegularExpressions;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class MainScreenWorkflowTests
{
    [Fact]
    public async Task RunScanAsync_ShouldReturnUiReadyResult_WriteAudit_AndCaptureHistory()
    {
        var auditWriter = new FakeAuditWriter();
        var historyRepository = new FakeHistoryRepository();
        var logger = new RecordingDiagnosticLogger();
        var workflow = new MainScreenWorkflow(
            new FakeScanOrchestrator(),
            new FakeRecommendationPipeline(),
            new FakeProviderCatalogSummaryService(),
            new FakeSettingsRepository(),
            logger,
            auditWriter,
            new MainScreenResultAssembler(
                new RecommendationDetailAssembler(),
                new OfficialSourceActionService(new OfficialSourceResolutionService([]), new OpenOfficialSourceActionEvaluator(), logger),
                new ShareableReportBuilder()),
            new ScanSessionHistoryService(historyRepository));

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
        Assert.Matches(
            new Regex(@"^driverguardian-scan-report-\d{8}-\d{6}Z-[a-f0-9]{32}$", RegexOptions.CultureInvariant),
            result.ReportExportPayload.FileNameBase);
        Assert.DoesNotContain(":", result.ReportExportPayload.FileNameBase, StringComparison.Ordinal);
        Assert.DoesNotContain(" ", result.ReportExportPayload.FileNameBase, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(result.ReportExportPayload.PlainTextContent));
        Assert.Contains("DriverGuardian Shareable Scan Report", result.ReportExportPayload.MarkdownContent);
        Assert.Single(result.RecommendationDetails);
        Assert.False(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionOutcome.InsufficientEvidence, result.OfficialSourceAction.ResolutionOutcome);
        Assert.Equal(3, result.RecentHistory.Count);
        Assert.Equal(37, historyRepository.LastTrimMaxEntries);
        Assert.Equal(37, historyRepository.LastRequestedTake);
        Assert.Equal(3, historyRepository.Entries.Count);
        Assert.Contains(historyRepository.Entries, entry => entry is ScanHistoryEntry);
        Assert.Contains(historyRepository.Entries, entry => entry is RecommendationSummaryHistoryEntry);
        Assert.Contains(historyRepository.Entries, entry => entry is VerificationHistoryEntry);
        Assert.Single(auditWriter.Entries);
        Assert.Contains(logger.InfoEvents, entry => entry.StartsWith("scan.workflow.start:", StringComparison.Ordinal));
        Assert.Contains(logger.InfoEvents, entry => entry.StartsWith("scan.discovery.completed:", StringComparison.Ordinal));
        Assert.Contains(logger.InfoEvents, entry => entry.StartsWith("scan.inspection.completed:", StringComparison.Ordinal));
        Assert.Contains(logger.InfoEvents, entry => entry.StartsWith("scan.recommendation.completed:", StringComparison.Ordinal));
        Assert.Contains(logger.InfoEvents, entry => entry.StartsWith("scan.official_source.state:", StringComparison.Ordinal));
        Assert.Contains(logger.InfoEvents, entry => entry.StartsWith("scan.history_report.completed:", StringComparison.Ordinal));
        Assert.Contains(logger.InfoEvents, entry => entry.StartsWith("scan.workflow.summary:", StringComparison.Ordinal));
        Assert.Empty(logger.WarningEvents);
        Assert.Empty(logger.ErrorEvents);
    }

    [Fact]
    public async Task RunScanAsync_WhenScanFails_ShouldLogErrorAndRethrow()
    {
        var logger = new RecordingDiagnosticLogger();
        var historyRepository = new FakeHistoryRepository();
        var workflow = new MainScreenWorkflow(
            new ThrowingScanOrchestrator(),
            new FakeRecommendationPipeline(),
            new FakeProviderCatalogSummaryService(),
            new FakeSettingsRepository(),
            logger,
            new FakeAuditWriter(),
            new MainScreenResultAssembler(
                new RecommendationDetailAssembler(),
                new OfficialSourceActionService(new OfficialSourceResolutionService([]), new OpenOfficialSourceActionEvaluator(), logger),
                new ShareableReportBuilder()),
            new ScanSessionHistoryService(historyRepository));

        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.RunScanAsync(CancellationToken.None));
        Assert.Single(logger.ErrorEvents);
        Assert.StartsWith("scan.workflow.failed:", logger.ErrorEvents[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunScanAsync_ShouldFilterLowValueDevicesAndKeepUserRelevantResults()
    {
        var historyRepository = new FakeHistoryRepository();
        var logger = new RecordingDiagnosticLogger();
        var workflow = new MainScreenWorkflow(
            new MixedScanOrchestrator(),
            new NoRecommendationPipeline(),
            new FakeProviderCatalogSummaryService(),
            new FakeSettingsRepository(),
            logger,
            new FakeAuditWriter(),
            new MainScreenResultAssembler(
                new RecommendationDetailAssembler(),
                new OfficialSourceActionService(new OfficialSourceResolutionService([]), new OpenOfficialSourceActionEvaluator(), logger),
                new ShareableReportBuilder()),
            new ScanSessionHistoryService(historyRepository));

        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Single(result.RecommendationDetails);
        Assert.Equal("Intel (Display)", result.RecommendationDetails.First().DeviceDisplayName);
        Assert.Equal("PCI\\VEN_8086&DEV_1234", result.RecommendationDetails.First().DeviceId);
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

            return Task.FromResult(new ScanResult(session, 2, discoveredDevices, drivers, ScanExecutionStatus.Completed, []));
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

    private sealed class FakeHistoryRepository : IResultHistoryRepository
    {
        public List<ResultHistoryEntry> Entries { get; } = [];

        public int LastRequestedTake { get; private set; }

        public int LastTrimMaxEntries { get; private set; }

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

        public Task TrimToMaxEntriesAsync(int maxEntries, CancellationToken cancellationToken)
        {
            LastTrimMaxEntries = maxEntries;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingScanOrchestrator : IScanOrchestrator
    {
        public Task<ScanResult> RunAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException("scan failure");
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

            return Task.FromResult(new ScanResult(session, 2, discoveredDevices, drivers, ScanExecutionStatus.Completed, []));
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
        public List<string> InfoEvents { get; } = [];
        public List<string> WarningEvents { get; } = [];
        public List<string> ErrorEvents { get; } = [];

        public Task LogInfoAsync(string eventName, string message, CancellationToken cancellationToken)
        {
            InfoEvents.Add($"{eventName}:{message}");
            return Task.CompletedTask;
        }

        public Task LogWarningAsync(string eventName, string message, CancellationToken cancellationToken)
        {
            WarningEvents.Add($"{eventName}:{message}");
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(string eventName, string message, Exception exception, CancellationToken cancellationToken)
        {
            ErrorEvents.Add($"{eventName}:{message}:{exception.GetType().Name}");
            return Task.CompletedTask;
        }
    }
}
