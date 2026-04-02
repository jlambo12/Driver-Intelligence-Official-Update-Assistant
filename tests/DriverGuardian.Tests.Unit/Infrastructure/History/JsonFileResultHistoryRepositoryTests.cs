using DriverGuardian.Application.History.Models;
using DriverGuardian.Infrastructure.History;

namespace DriverGuardian.Tests.Unit.Infrastructure.History;

public sealed class JsonFileResultHistoryRepositoryTests
{
    [Fact]
    public async Task GetRecentAsync_WhenFileDoesNotExist_ShouldReturnEmpty()
    {
        var filePath = CreateTemporaryHistoryFilePath();
        var repository = new JsonFileResultHistoryRepository(filePath);

        var recent = await repository.GetRecentAsync(10, CancellationToken.None);

        Assert.Empty(recent);
    }

    [Fact]
    public async Task SaveAsync_ThenCreateNewInstance_ShouldReloadEntries()
    {
        var filePath = CreateTemporaryHistoryFilePath();

        var writer = new JsonFileResultHistoryRepository(filePath);
        await writer.SaveAsync(
            ScanHistoryEntry.Create(
                Guid.NewGuid(),
                new DateTimeOffset(2026, 2, 12, 8, 0, 0, TimeSpan.Zero),
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                discoveredDeviceCount: 3,
                inspectedDriverCount: 2),
            CancellationToken.None);

        await writer.SaveAsync(
            RecommendationSummaryHistoryEntry.Create(
                Guid.NewGuid(),
                new DateTimeOffset(2026, 2, 12, 8, 1, 0, TimeSpan.Zero),
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                totalRecommendations: 2,
                requiresManualInstallCount: 1,
                deferredDecisionCount: 1),
            CancellationToken.None);

        var reloaded = new JsonFileResultHistoryRepository(filePath);
        var recent = await reloaded.GetRecentAsync(10, CancellationToken.None);

        Assert.Equal(2, recent.Count);
        Assert.Collection(recent,
            first => Assert.IsType<RecommendationSummaryHistoryEntry>(first),
            second => Assert.IsType<ScanHistoryEntry>(second));
    }

    [Fact]
    public async Task TrimToMaxEntriesAsync_ShouldKeepMostRecentEntriesOnly()
    {
        var filePath = CreateTemporaryHistoryFilePath();
        var repository = new JsonFileResultHistoryRepository(filePath);

        await repository.SaveAsync(
            ScanHistoryEntry.Create(Guid.NewGuid(), new DateTimeOffset(2026, 2, 12, 8, 0, 0, TimeSpan.Zero), Guid.NewGuid(), 1, 1),
            CancellationToken.None);
        await repository.SaveAsync(
            ScanHistoryEntry.Create(Guid.NewGuid(), new DateTimeOffset(2026, 2, 12, 8, 1, 0, TimeSpan.Zero), Guid.NewGuid(), 2, 2),
            CancellationToken.None);
        await repository.SaveAsync(
            VerificationHistoryEntry.Create(Guid.NewGuid(), new DateTimeOffset(2026, 2, 12, 8, 2, 0, TimeSpan.Zero), Guid.NewGuid(), VerificationHistoryStatus.Passed, "ok"),
            CancellationToken.None);

        await repository.TrimToMaxEntriesAsync(maxEntries: 2, CancellationToken.None);

        var recent = await repository.GetRecentAsync(10, CancellationToken.None);
        Assert.Equal(2, recent.Count);
        Assert.DoesNotContain(recent, entry => entry.OccurredAtUtc == new DateTimeOffset(2026, 2, 12, 8, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task SaveAsync_WithDuplicateIdentifier_ShouldReplaceOlderVersion()
    {
        var filePath = CreateTemporaryHistoryFilePath();
        var repository = new JsonFileResultHistoryRepository(filePath);
        var id = Guid.NewGuid();

        await repository.SaveAsync(
            ScanHistoryEntry.Create(id, new DateTimeOffset(2026, 2, 12, 8, 0, 0, TimeSpan.Zero), Guid.NewGuid(), 1, 1),
            CancellationToken.None);

        await repository.SaveAsync(
            ScanHistoryEntry.Create(id, new DateTimeOffset(2026, 2, 12, 9, 0, 0, TimeSpan.Zero), Guid.NewGuid(), 7, 6),
            CancellationToken.None);

        var recent = await repository.GetRecentAsync(5, CancellationToken.None);
        var scan = Assert.IsType<ScanHistoryEntry>(Assert.Single(recent));

        Assert.Equal(7, scan.DiscoveredDeviceCount);
        Assert.Equal(6, scan.InspectedDriverCount);
    }

    private static string CreateTemporaryHistoryFilePath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DriverGuardian.Tests", Guid.NewGuid().ToString("N"));
        return Path.Combine(directory, "result-history.json");
    }
}
