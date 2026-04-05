using System.Text.Json;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

public interface IWindowsCatalogDataSource
{
    WindowsCatalogDataset Load();
}

public sealed record CatalogDriverRecord(
    string DriverIdentifier,
    string CandidateVersion,
    Uri SourceUri,
    string PublisherName,
    string EvidenceNote);

public sealed record WindowsCatalogDataset(
    string SourceName,
    IReadOnlyDictionary<string, CatalogDriverRecord> CatalogByHardwareId,
    IReadOnlyDictionary<string, CatalogDriverRecord> CatalogByVendorId)
{
    public static WindowsCatalogDataset Empty(string sourceName)
        => new(sourceName, new Dictionary<string, CatalogDriverRecord>(StringComparer.OrdinalIgnoreCase), new Dictionary<string, CatalogDriverRecord>(StringComparer.OrdinalIgnoreCase));
}

public sealed class SnapshotWindowsCatalogDataSource : IWindowsCatalogDataSource
{
    private const string SnapshotFileRelativePath = "Data/windows-catalog-snapshot.json";

    public WindowsCatalogDataset Load()
    {
        var catalog = LoadCatalogByHardwareId();
        var byVendor = BuildVendorFallbackMap(catalog);
        return new WindowsCatalogDataset("embedded-snapshot", catalog, byVendor);
    }

    private static IReadOnlyDictionary<string, CatalogDriverRecord> LoadCatalogByHardwareId()
    {
        var snapshotPath = ResolveSnapshotPath();
        if (!File.Exists(snapshotPath))
        {
            return new Dictionary<string, CatalogDriverRecord>(StringComparer.OrdinalIgnoreCase);
        }

        List<SnapshotRecord>? snapshot;
        try
        {
            using var stream = File.OpenRead(snapshotPath);
            snapshot = JsonSerializer.Deserialize<List<SnapshotRecord>>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (IOException)
        {
            return new Dictionary<string, CatalogDriverRecord>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, CatalogDriverRecord>(StringComparer.OrdinalIgnoreCase);
        }

        if (snapshot is null || snapshot.Count == 0)
        {
            return new Dictionary<string, CatalogDriverRecord>(StringComparer.OrdinalIgnoreCase);
        }

        var catalog = new Dictionary<string, CatalogDriverRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in snapshot)
        {
            if (string.IsNullOrWhiteSpace(entry.HardwareId) ||
                string.IsNullOrWhiteSpace(entry.DriverIdentifier) ||
                string.IsNullOrWhiteSpace(entry.CandidateVersion) ||
                string.IsNullOrWhiteSpace(entry.SourceUri))
            {
                continue;
            }

            if (!Uri.TryCreate(entry.SourceUri, UriKind.Absolute, out var sourceUri))
            {
                continue;
            }

            catalog[entry.HardwareId.Trim()] = new CatalogDriverRecord(
                DriverIdentifier: entry.DriverIdentifier.Trim(),
                CandidateVersion: entry.CandidateVersion.Trim(),
                SourceUri: sourceUri,
                PublisherName: string.IsNullOrWhiteSpace(entry.PublisherName)
                    ? "Microsoft Update Catalog"
                    : entry.PublisherName.Trim(),
                EvidenceNote: string.IsNullOrWhiteSpace(entry.EvidenceNote)
                    ? "Matched hardware id against bundled Windows Update Catalog snapshot (partial coverage)."
                    : entry.EvidenceNote.Trim());
        }

        return catalog;
    }

    private static string ResolveSnapshotPath()
    {
        var overridePath = Environment.GetEnvironmentVariable("DRIVER_GUARDIAN_WINDOWS_CATALOG_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath.Trim());
        }

        return Path.Combine(AppContext.BaseDirectory, SnapshotFileRelativePath);
    }

    private static IReadOnlyDictionary<string, CatalogDriverRecord> BuildVendorFallbackMap(
        IReadOnlyDictionary<string, CatalogDriverRecord> catalogByHardwareId)
    {
        var map = new Dictionary<string, CatalogDriverRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in catalogByHardwareId)
        {
            if (!TryExtractVendorToken(entry.Key, out var vendorToken) || string.IsNullOrWhiteSpace(vendorToken))
            {
                continue;
            }

            if (!map.ContainsKey(vendorToken))
            {
                map[vendorToken] = entry.Value;
            }
        }

        return map;
    }

    private static bool TryExtractVendorToken(string hardwareId, out string? token)
    {
        if (hardwareId.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase) &&
            TryExtractToken(hardwareId, "VEN_", out var ven))
        {
            token = $"PCI:{ven}";
            return true;
        }

        if (hardwareId.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase) &&
            TryExtractToken(hardwareId, "VID_", out var vid))
        {
            token = $"USB:{vid}";
            return true;
        }

        token = null;
        return false;
    }

    private static bool TryExtractToken(string value, string key, out string? token)
    {
        token = null;
        var start = value.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (start < 0 || start + key.Length + 4 > value.Length)
        {
            return false;
        }

        var raw = value.Substring(start + key.Length, 4);
        if (!raw.All(Uri.IsHexDigit))
        {
            return false;
        }

        token = raw.ToUpperInvariant();
        return true;
    }

    private sealed record SnapshotRecord(
        string HardwareId,
        string DriverIdentifier,
        string CandidateVersion,
        string SourceUri,
        string PublisherName,
        string EvidenceNote);
}
