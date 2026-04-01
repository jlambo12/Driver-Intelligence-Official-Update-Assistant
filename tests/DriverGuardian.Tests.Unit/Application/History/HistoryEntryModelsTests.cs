using DriverGuardian.Application.History.Models;

namespace DriverGuardian.Tests.Unit.Application.History;

public sealed class HistoryEntryModelsTests
{
    [Fact]
    public void ScanHistoryEntry_Create_ShouldCaptureCounts()
    {
        var scanSessionId = Guid.NewGuid();

        var entry = ScanHistoryEntry.Create(
            id: Guid.NewGuid(),
            occurredAtUtc: new DateTimeOffset(2026, 1, 10, 8, 15, 0, TimeSpan.Zero),
            scanSessionId: scanSessionId,
            discoveredDeviceCount: 3,
            inspectedDriverCount: 2);

        Assert.Equal(scanSessionId, entry.ScanSessionId);
        Assert.Equal(3, entry.DiscoveredDeviceCount);
        Assert.Equal(2, entry.InspectedDriverCount);
    }

    [Fact]
    public void RecommendationSummaryHistoryEntry_Create_ShouldRejectManualCountExceedingTotal()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RecommendationSummaryHistoryEntry.Create(
            id: Guid.NewGuid(),
            occurredAtUtc: new DateTimeOffset(2026, 1, 10, 8, 15, 0, TimeSpan.Zero),
            scanSessionId: Guid.NewGuid(),
            totalRecommendations: 2,
            requiresManualInstallCount: 3,
            deferredDecisionCount: 0));
    }

    [Fact]
    public void VerificationHistoryEntry_Create_ShouldTrimNote()
    {
        var entry = VerificationHistoryEntry.Create(
            id: Guid.NewGuid(),
            occurredAtUtc: new DateTimeOffset(2026, 1, 10, 8, 15, 0, TimeSpan.Zero),
            scanSessionId: Guid.NewGuid(),
            status: VerificationHistoryStatus.Unknown,
            note: "  waiting for implementation  ");

        Assert.Equal("waiting for implementation", entry.Note);
        Assert.Equal(VerificationHistoryStatus.Unknown, entry.Status);
    }
}
