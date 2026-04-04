using System.Text.Json;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

/// <summary>
/// Provider adapter backed by a curated Windows Update Catalog snapshot.
/// Coverage is intentionally narrow and returns empty results when no catalog match is found
/// (including normalized PCI/USB hardware-id fallback).
/// </summary>
public sealed class OfficialWindowsCatalogProviderAdapter : IOfficialProviderAdapter
{
    private const string SnapshotFileRelativePath = "Data/windows-catalog-snapshot.json";
    private const string PciPrefix = "PCI\\";
    private const string UsbPrefix = "USB\\";
    private const string PciVendorTokenPrefix = "PCI:";
    private const string UsbVendorTokenPrefix = "USB:";
    private static readonly IReadOnlyDictionary<string, CatalogDriverRecord> CatalogByHardwareId = LoadSnapshot();
    private static readonly IReadOnlyDictionary<string, CatalogDriverRecord> CatalogByVendorId = BuildVendorFallbackMap();

    public ProviderDescriptor Descriptor => new(
        Code: "windows-update-catalog",
        DisplayName: "Windows Update Catalog (Curated Snapshot)",
        IsEnabled: true,
        OfficialSourceOnly: true,
        Precedence: ProviderPrecedence.PlatformVendor);

    public Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (request.HardwareIds.Count == 0)
            {
                return Task.FromResult(new ProviderLookupResponse(
                    ProviderCode: Descriptor.Code,
                    IsSuccess: false,
                    Candidates: [],
                    FailureReason: "At least one hardware id is required for provider lookup."));
            }

            CatalogMatch? bestMatch = null;
            foreach (var hardwareId in request.HardwareIds)
            {
                if (string.IsNullOrWhiteSpace(hardwareId))
                {
                    continue;
                }

                if (!TryResolveCatalogRecord(hardwareId, out var record, out var matchType, out var compatibilityConfidence, out var score))
                {
                    continue;
                }

                var match = new CatalogMatch(record, matchType, compatibilityConfidence, score, hardwareId);
                if (bestMatch is null || match.Score > bestMatch.Score)
                {
                    bestMatch = match;
                }
            }

            if (bestMatch is not null)
            {
                var candidate = new ProviderCandidate(
                    DriverIdentifier: bestMatch.Record.DriverIdentifier,
                    CandidateVersion: bestMatch.Record.CandidateVersion,
                    ReleaseDateIso: null,
                    CompatibilityConfidence: bestMatch.CompatibilityConfidence,
                    SourceEvidence: new SourceEvidence(
                        bestMatch.Record.SourceUri,
                        bestMatch.Record.PublisherName,
                        SourceTrustLevel.OperatingSystemCatalog,
                        IsOfficialSource: true,
                        $"{bestMatch.Record.EvidenceNote} Match type: {bestMatch.MatchType}; input: {bestMatch.InputHardwareId}."),
                    DownloadUri: null);

                return Task.FromResult(new ProviderLookupResponse(
                    ProviderCode: Descriptor.Code,
                    IsSuccess: true,
                    Candidates: [candidate],
                    FailureReason: null));
            }

            return Task.FromResult(new ProviderLookupResponse(
                ProviderCode: Descriptor.Code,
                IsSuccess: true,
                Candidates: [],
                FailureReason: null));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ProviderLookupResponse(
                ProviderCode: Descriptor.Code,
                IsSuccess: false,
                Candidates: [],
                FailureReason: $"Provider lookup failed: {ex.Message}"));
        }
    }

    private sealed record CatalogDriverRecord(
        string DriverIdentifier,
        string CandidateVersion,
        Uri SourceUri,
        string PublisherName,
        string EvidenceNote);

    private sealed record SnapshotRecord(
        string HardwareId,
        string DriverIdentifier,
        string CandidateVersion,
        string SourceUri,
        string PublisherName,
        string EvidenceNote);

    private static IReadOnlyDictionary<string, CatalogDriverRecord> LoadSnapshot()
    {
        var snapshotPath = Path.Combine(AppContext.BaseDirectory, SnapshotFileRelativePath);
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
                    ? "Matched exact hardware id against bundled Windows Update Catalog snapshot (partial coverage)."
                    : entry.EvidenceNote.Trim());
        }

        return catalog;
    }

    private static IReadOnlyDictionary<string, CatalogDriverRecord> BuildVendorFallbackMap()
    {
        var map = new Dictionary<string, CatalogDriverRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in CatalogByHardwareId)
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

    private sealed record CatalogMatch(
        CatalogDriverRecord Record,
        string MatchType,
        CompatibilityConfidence CompatibilityConfidence,
        int Score,
        string InputHardwareId);

    private static bool TryResolveCatalogRecord(
        string hardwareId,
        out CatalogDriverRecord record,
        out string matchType,
        out CompatibilityConfidence compatibilityConfidence,
        out int score)
    {
        record = default!;
        matchType = "none";
        compatibilityConfidence = CompatibilityConfidence.Unknown;
        score = 0;

        var trimmed = hardwareId.Trim();
        if (CatalogByHardwareId.TryGetValue(trimmed, out record))
        {
            matchType = "exact";
            compatibilityConfidence = CompatibilityConfidence.High;
            score = 300;
            return true;
        }

        var normalized = NormalizeHardwareId(trimmed);
        if (normalized is null || string.Equals(normalized, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            return TryResolveVendorFallback(trimmed, out record, out matchType, out compatibilityConfidence, out score);
        }

        if (CatalogByHardwareId.TryGetValue(normalized, out record))
        {
            matchType = "normalized";
            compatibilityConfidence = CompatibilityConfidence.Medium;
            score = 200;
            return true;
        }

        return TryResolveVendorFallback(trimmed, out record, out matchType, out compatibilityConfidence, out score);
    }

    private static string? NormalizeHardwareId(string hardwareId)
    {
        if (hardwareId.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase))
        {
            var ven = ExtractToken(hardwareId, "VEN_");
            var dev = ExtractToken(hardwareId, "DEV_");
            if (ven is not null && dev is not null)
            {
                return $"PCI\\VEN_{ven}&DEV_{dev}";
            }
        }

        if (hardwareId.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase))
        {
            var vid = ExtractToken(hardwareId, "VID_");
            var pid = ExtractToken(hardwareId, "PID_");
            if (vid is not null && pid is not null)
            {
                return $"USB\\VID_{vid}&PID_{pid}";
            }
        }

        return null;
    }

    private static bool TryResolveVendorFallback(
        string hardwareId,
        out CatalogDriverRecord record,
        out string matchType,
        out CompatibilityConfidence compatibilityConfidence,
        out int score)
    {
        record = default!;
        matchType = "none";
        compatibilityConfidence = CompatibilityConfidence.Unknown;
        score = 0;

        if (!TryExtractVendorToken(hardwareId, out var vendorToken) || string.IsNullOrWhiteSpace(vendorToken))
        {
            return false;
        }

        if (!CatalogByVendorId.TryGetValue(vendorToken, out record))
        {
            return false;
        }

        matchType = "compatible-vendor";
        compatibilityConfidence = CompatibilityConfidence.Low;
        score = 100;
        return true;
    }

    private static bool TryExtractVendorToken(string hardwareId, out string? token)
    {
        if (hardwareId.StartsWith(PciPrefix, StringComparison.OrdinalIgnoreCase) &&
            TryExtractToken(hardwareId, "VEN_", out var ven))
        {
            token = $"{PciVendorTokenPrefix}{ven}";
            return true;
        }

        if (hardwareId.StartsWith(UsbPrefix, StringComparison.OrdinalIgnoreCase) &&
            TryExtractToken(hardwareId, "VID_", out var vid))
        {
            token = $"{UsbVendorTokenPrefix}{vid}";
            return true;
        }

        token = null;
        return false;
    }

    private static string? ExtractToken(string value, string key)
        => TryExtractToken(value, key, out var token) ? token : null;

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
}
