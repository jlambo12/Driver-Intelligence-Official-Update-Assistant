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
    private static readonly IReadOnlyDictionary<string, CatalogDriverRecord> CatalogByHardwareId =
        new Dictionary<string, CatalogDriverRecord>(StringComparer.OrdinalIgnoreCase)
        {
            ["PCI\\VEN_8086&DEV_15F3"] = new CatalogDriverRecord(
                DriverIdentifier: "windows-update:8086-15f3:31.0.101.2125",
                CandidateVersion: "31.0.101.2125",
                SourceUri: new Uri("https://www.catalog.update.microsoft.com/Search.aspx?q=PCI%5CVEN_8086%26DEV_15F3"),
                PublisherName: "Microsoft Update Catalog",
                EvidenceNote: "Matched exact hardware id against bundled Windows Update Catalog snapshot (partial coverage)."),
            ["PCI\\VEN_10EC&DEV_8168"] = new CatalogDriverRecord(
                DriverIdentifier: "windows-update:10ec-8168:10.68.615.2022",
                CandidateVersion: "10.68.615.2022",
                SourceUri: new Uri("https://www.catalog.update.microsoft.com/Search.aspx?q=PCI%5CVEN_10EC%26DEV_8168"),
                PublisherName: "Microsoft Update Catalog",
                EvidenceNote: "Matched exact hardware id against bundled Windows Update Catalog snapshot (partial coverage)."),
            ["PCI\\VEN_8086&DEV_51F0"] = new CatalogDriverRecord(
                DriverIdentifier: "windows-update:8086-51f0:31.0.101.5522",
                CandidateVersion: "31.0.101.5522",
                SourceUri: new Uri("https://www.catalog.update.microsoft.com/Search.aspx?q=PCI%5CVEN_8086%26DEV_51F0"),
                PublisherName: "Microsoft Update Catalog",
                EvidenceNote: "Matched exact hardware id against bundled Windows Update Catalog snapshot (partial coverage)."),
            ["PCI\\VEN_10DE&DEV_1C82"] = new CatalogDriverRecord(
                DriverIdentifier: "windows-update:10de-1c82:32.0.15.6109",
                CandidateVersion: "32.0.15.6109",
                SourceUri: new Uri("https://www.catalog.update.microsoft.com/Search.aspx?q=PCI%5CVEN_10DE%26DEV_1C82"),
                PublisherName: "Microsoft Update Catalog",
                EvidenceNote: "Matched exact hardware id against bundled Windows Update Catalog snapshot (partial coverage)."),
            ["USB\\VID_0BDA&PID_8153"] = new CatalogDriverRecord(
                DriverIdentifier: "windows-update:0bda-8153:10.63.20.1028",
                CandidateVersion: "10.63.20.1028",
                SourceUri: new Uri("https://www.catalog.update.microsoft.com/Search.aspx?q=USB%5CVID_0BDA%26PID_8153"),
                PublisherName: "Microsoft Update Catalog",
                EvidenceNote: "Matched exact hardware id against bundled Windows Update Catalog snapshot (partial coverage).")
        };

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

            foreach (var hardwareId in request.HardwareIds)
            {
                if (string.IsNullOrWhiteSpace(hardwareId))
                {
                    continue;
                }

                if (!TryResolveCatalogRecord(hardwareId, out var record, out var matchType))
                {
                    continue;
                }

                var candidate = new ProviderCandidate(
                    DriverIdentifier: record.DriverIdentifier,
                    CandidateVersion: record.CandidateVersion,
                    ReleaseDateIso: null,
                    CompatibilityConfidence: CompatibilityConfidence.Medium,
                    SourceEvidence: new SourceEvidence(
                        record.SourceUri,
                        record.PublisherName,
                        SourceTrustLevel.OperatingSystemCatalog,
                        IsOfficialSource: true,
                        $"{record.EvidenceNote} Match type: {matchType}."),
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

    private static bool TryResolveCatalogRecord(string hardwareId, out CatalogDriverRecord record, out string matchType)
    {
        record = default!;
        matchType = "none";

        var trimmed = hardwareId.Trim();
        if (CatalogByHardwareId.TryGetValue(trimmed, out record))
        {
            matchType = "exact";
            return true;
        }

        var normalized = NormalizeHardwareId(trimmed);
        if (normalized is null || string.Equals(normalized, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (CatalogByHardwareId.TryGetValue(normalized, out record))
        {
            matchType = "normalized";
            return true;
        }

        return false;
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

    private static string? ExtractToken(string value, string key)
    {
        var start = value.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (start < 0 || start + key.Length + 4 > value.Length)
        {
            return null;
        }

        var token = value.Substring(start + key.Length, 4);
        return token.All(Uri.IsHexDigit) ? token.ToUpperInvariant() : null;
    }
}
