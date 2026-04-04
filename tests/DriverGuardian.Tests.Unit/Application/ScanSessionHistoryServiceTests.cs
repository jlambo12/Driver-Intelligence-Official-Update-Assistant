using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.History;
using DriverGuardian.Application.History.Models;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.Reports;
using DriverGuardian.Application.Verification;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Settings;
using DriverGuardian.Domain.Scanning;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class ScanSessionHistoryServiceTests
{
    [Fact]
    public async Task RecordAndTrimAsync_ShouldMarkVerificationAsFailed_WhenVerificationOutcomesContainNoChange()
    {
        var repository = new RecordingResultHistoryRepository();
        var service = new ScanSessionHistoryService(repository);

        await service.RecordAndTrimAsync(
            CreateScanResult(),
            recommendationCount: 1,
            manualHandoffUserActionCount: 0,
            notRecommendedCount: 0,
            verifications:
            [
                CreateVerificationReportItem(PostInstallVerificationOutcome.NoChangeDetected)
            ],
            verificationSummary: "verification summary",
            settings: AppSettings.Default,
            cancellationToken: CancellationToken.None);

        var verificationEntry = Assert.IsType<VerificationHistoryEntry>(
            repository.SavedEntries.Single(entry => entry is VerificationHistoryEntry));
        Assert.Equal(VerificationHistoryStatus.Failed, verificationEntry.Status);
    }

    [Fact]
    public async Task RecordAndTrimAsync_ShouldMarkVerificationAsSkipped_WhenManualHandoffExistsAndNoVerificationYet()
    {
        var repository = new RecordingResultHistoryRepository();
        var service = new ScanSessionHistoryService(repository);

        await service.RecordAndTrimAsync(
            CreateScanResult(),
            recommendationCount: 1,
            manualHandoffUserActionCount: 2,
            notRecommendedCount: 0,
            verifications: [],
            verificationSummary: "verification summary",
            settings: AppSettings.Default,
            cancellationToken: CancellationToken.None);

        var verificationEntry = Assert.IsType<VerificationHistoryEntry>(
            repository.SavedEntries.Single(entry => entry is VerificationHistoryEntry));
        Assert.Equal(VerificationHistoryStatus.Skipped, verificationEntry.Status);
    }

    private static ScanResult CreateScanResult()
    {
        var session = ScanSession.Start(Guid.NewGuid(), DateTimeOffset.UtcNow).Complete(DateTimeOffset.UtcNow);
        return new ScanResult(
            Session: session,
            DiscoveredDeviceCount: 0,
            DiscoveredDevices: [],
            Drivers: [],
            ExecutionStatus: ScanExecutionStatus.Completed,
            Issues: []);
    }

    private static VerificationReportItem CreateVerificationReportItem(PostInstallVerificationOutcome outcome)
    {
        return new VerificationReportItem(
            new DeviceIdentity("PCI\\VEN_1234&DEV_5678"),
            new PostInstallVerificationResult(
                outcome,
                PostInstallVerificationReason.None,
                Comparison: null,
                Message: "test"));
    }

    private sealed class RecordingResultHistoryRepository : IResultHistoryRepository
    {
        public List<ResultHistoryEntry> SavedEntries { get; } = [];

        public Task SaveAsync(ResultHistoryEntry entry, CancellationToken cancellationToken)
        {
            SavedEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ResultHistoryEntry>> GetRecentAsync(int take, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ResultHistoryEntry>>([]);

        public Task TrimToMaxEntriesAsync(int maxEntries, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
