using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

/// <summary>
/// Provider adapter backed by a catalog source (snapshot by default).
/// Coverage is explicitly best-effort: only exact hardware-id matches are treated as high-confidence.
/// </summary>
public sealed class OfficialWindowsCatalogProviderAdapter : IOfficialProviderAdapter
{
    private const string PciPrefix = "PCI\\";
    private const string UsbPrefix = "USB\\";
    private const string PciVendorTokenPrefix = "PCI:";
    private const string UsbVendorTokenPrefix = "USB:";

    private readonly WindowsCatalogDataset _dataset;

    public OfficialWindowsCatalogProviderAdapter(IWindowsCatalogDataSource? dataSource = null)
    {
        var source = dataSource ?? new SnapshotWindowsCatalogDataSource();
        _dataset = source.Load();
    }

    public ProviderDescriptor Descriptor => new(
        Code: "windows-update-catalog",
        DisplayName: "Windows Update Catalog (Data Source Backed)",
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

                if (!TryResolveCatalogRecord(hardwareId, out var record, out var matchType, out var compatibilityConfidence, out var score, out var matchQuality))
                {
                    continue;
                }

                var match = new CatalogMatch(record, matchType, compatibilityConfidence, score, hardwareId, matchQuality);
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
                    HardwareMatchQuality: bestMatch.HardwareMatchQuality,
                    SourceEvidence: new SourceEvidence(
                        bestMatch.Record.SourceUri,
                        bestMatch.Record.PublisherName,
                        SourceTrustLevel.OperatingSystemCatalog,
                        IsOfficialSource: true,
                        $"{bestMatch.Record.EvidenceNote} Match type: {bestMatch.MatchType}; input: {bestMatch.InputHardwareId}; catalog source: {_dataset.SourceName}."),
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

    private sealed record CatalogMatch(
        CatalogDriverRecord Record,
        string MatchType,
        CompatibilityConfidence CompatibilityConfidence,
        int Score,
        string InputHardwareId,
        HardwareMatchQuality HardwareMatchQuality);

    private bool TryResolveCatalogRecord(
        string hardwareId,
        out CatalogDriverRecord record,
        out string matchType,
        out CompatibilityConfidence compatibilityConfidence,
        out int score,
        out HardwareMatchQuality hardwareMatchQuality)
    {
        record = default!;
        matchType = "none";
        compatibilityConfidence = CompatibilityConfidence.Unknown;
        score = 0;
        hardwareMatchQuality = HardwareMatchQuality.Unknown;

        var trimmed = hardwareId.Trim();
        if (_dataset.CatalogByHardwareId.TryGetValue(trimmed, out var exactRecord) && exactRecord is not null)
        {
            record = exactRecord;
            matchType = "exact";
            compatibilityConfidence = CompatibilityConfidence.High;
            score = 300;
            hardwareMatchQuality = HardwareMatchQuality.ExactHardwareId;
            return true;
        }

        var normalized = NormalizeHardwareId(trimmed);
        if (normalized is not null &&
            !string.Equals(normalized, trimmed, StringComparison.OrdinalIgnoreCase) &&
            _dataset.CatalogByHardwareId.TryGetValue(normalized, out var normalizedRecord) &&
            normalizedRecord is not null)
        {
            record = normalizedRecord;
            matchType = "normalized";
            compatibilityConfidence = CompatibilityConfidence.Medium;
            score = 200;
            hardwareMatchQuality = HardwareMatchQuality.NormalizedHardwareId;
            return true;
        }

        return TryResolveVendorFallback(trimmed, out record, out matchType, out compatibilityConfidence, out score, out hardwareMatchQuality);
    }

    private bool TryResolveVendorFallback(
        string hardwareId,
        out CatalogDriverRecord record,
        out string matchType,
        out CompatibilityConfidence compatibilityConfidence,
        out int score,
        out HardwareMatchQuality hardwareMatchQuality)
    {
        record = default!;
        matchType = "none";
        compatibilityConfidence = CompatibilityConfidence.Unknown;
        score = 0;
        hardwareMatchQuality = HardwareMatchQuality.Unknown;

        if (!TryExtractVendorToken(hardwareId, out var vendorToken) || string.IsNullOrWhiteSpace(vendorToken))
        {
            return false;
        }

        if (!_dataset.CatalogByVendorId.TryGetValue(vendorToken, out var vendorRecord) || vendorRecord is null)
        {
            return false;
        }

        record = vendorRecord;
        matchType = "vendor-family-fallback";
        compatibilityConfidence = CompatibilityConfidence.Unknown;
        score = 100;
        hardwareMatchQuality = HardwareMatchQuality.VendorFamilyFallback;
        return true;
    }

    private static string? NormalizeHardwareId(string hardwareId)
    {
        if (hardwareId.StartsWith(PciPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var ven = ExtractToken(hardwareId, "VEN_");
            var dev = ExtractToken(hardwareId, "DEV_");
            if (ven is not null && dev is not null)
            {
                return $"PCI\\VEN_{ven}&DEV_{dev}";
            }
        }

        if (hardwareId.StartsWith(UsbPrefix, StringComparison.OrdinalIgnoreCase))
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
