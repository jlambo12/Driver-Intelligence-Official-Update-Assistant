using System.Text.Json;

namespace DriverGuardian.Tests.Unit.ProviderAdapters.Official;

public sealed class WindowsCatalogSnapshotCoverageTests
{
    [Fact]
    public void Snapshot_ShouldHaveBroadHardwareCoverageAndVendorDiversity()
    {
        var snapshotPath = Path.Combine(AppContext.BaseDirectory, "Data", "windows-catalog-snapshot.json");
        Assert.True(File.Exists(snapshotPath), $"Snapshot file was not found at '{snapshotPath}'.");

        using var stream = File.OpenRead(snapshotPath);
        var entries = JsonSerializer.Deserialize<List<SnapshotRecord>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(entries);
        Assert.True(entries!.Count >= 50, $"Expected at least 50 snapshot records, but got {entries.Count}.");

        var uniqueHardwareIds = entries
            .Where(x => !string.IsNullOrWhiteSpace(x.HardwareId))
            .Select(x => x.HardwareId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.Equal(entries.Count, uniqueHardwareIds.Length);

        var vendorCount = entries
            .Where(x => !string.IsNullOrWhiteSpace(x.PublisherName))
            .Select(x => x.PublisherName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        Assert.True(vendorCount >= 12, $"Expected at least 12 vendors, but got {vendorCount}.");
    }

    private sealed record SnapshotRecord(
        string HardwareId,
        string DriverIdentifier,
        string CandidateVersion,
        string SourceUri,
        string PublisherName,
        string EvidenceNote);
}
