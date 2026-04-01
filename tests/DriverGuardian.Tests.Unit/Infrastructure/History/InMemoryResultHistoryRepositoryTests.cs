using DriverGuardian.Application.History.Models;
using DriverGuardian.Infrastructure.History;

namespace DriverGuardian.Tests.Unit.Infrastructure.History;

public sealed class InMemoryResultHistoryRepositoryTests
{
    [Fact]
    public async Task SaveAsync_ThenGetRecentAsync_ShouldReturnEntriesInDescendingTimeOrder()
    {
        var repository = new InMemoryResultHistoryRepository();

        await repository.SaveAsync(
            ScanHistoryEntry.Create(
                id: Guid.NewGuid(),
                occurredAtUtc: new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
                scanSessionId: Guid.NewGuid(),
                discoveredDeviceCount: 1,
                inspectedDriverCount: 1),
            CancellationToken.None);

        await repository.SaveAsync(
            VerificationHistoryEntry.Create(
                id: Guid.NewGuid(),
                occurredAtUtc: new DateTimeOffset(2026, 1, 1, 11, 0, 0, TimeSpan.Zero),
                scanSessionId: Guid.NewGuid(),
                status: VerificationHistoryStatus.Unknown,
                note: null),
            CancellationToken.None);

        var recent = await repository.GetRecentAsync(take: 2, CancellationToken.None);

        Assert.Collection(recent,
            first => Assert.IsType<VerificationHistoryEntry>(first),
            second => Assert.IsType<ScanHistoryEntry>(second));
    }

    [Fact]
    public async Task SaveAsync_WithDuplicateIdentifier_ShouldReplaceOlderVersion()
    {
        var repository = new InMemoryResultHistoryRepository();
        var entryId = Guid.NewGuid();

        await repository.SaveAsync(
            ScanHistoryEntry.Create(
                id: entryId,
                occurredAtUtc: new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
                scanSessionId: Guid.NewGuid(),
                discoveredDeviceCount: 1,
                inspectedDriverCount: 1),
            CancellationToken.None);

        await repository.SaveAsync(
            ScanHistoryEntry.Create(
                id: entryId,
                occurredAtUtc: new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero),
                scanSessionId: Guid.NewGuid(),
                discoveredDeviceCount: 4,
                inspectedDriverCount: 4),
            CancellationToken.None);

        var recent = await repository.GetRecentAsync(take: 5, CancellationToken.None);
        var stored = Assert.Single(recent);
        var scanEntry = Assert.IsType<ScanHistoryEntry>(stored);

        Assert.Equal(4, scanEntry.DiscoveredDeviceCount);
    }
}
