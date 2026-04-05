using System.Text.Json;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

/// <summary>
/// Provider adapter backed by one or more Windows catalog data sources.
/// Default runtime uses an embedded snapshot and may optionally ingest an external JSON source
/// via DRIVER_GUARDIAN_WINDOWS_CATALOG_JSON.
/// </summary>
public sealed class OfficialWindowsCatalogProviderAdapter : IOfficialProviderAdapter
{
    private const string SnapshotFileRelativePath = "Data/windows-catalog-snapshot.json";
    private const string ExternalCatalogPathEnvironmentVariable = "DRIVER_GUARDIAN_WINDOWS_CATALOG_JSON";
    private const string PciPrefix = "PCI\\";
    private const string UsbPrefix = "USB\\";
    private const string PciVendorTokenPrefix = "PCI:";
    private const string UsbVendorTokenPrefix = "USB:";

    private readonly IReadOnlyDictionary<string, CatalogDriverRecord> _catalogByHardwareId;
    private readonly IReadOnlyDictionary<string, CatalogDriverRecord> _catalogByVendorId;

    public OfficialWindowsCatalogProviderAdapter()
        : this(CreateDefaultSources())
    {
    }

    public OfficialWindowsCatalogProviderAdapter(string externalCatalogJsonPath)
        : this(CreateDefaultSources(externalCatalogJsonPath))
    {
    }

    internal OfficialWindowsCatalogProviderAdapter(IEnumerable<IWindowsCatalogDataSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        _catalogByHardwareId = BuildCatalog(sources);
        _catalogByVendorId = BuildVendorFallbackMap(_catalogByHardwareId);
    }

    public ProviderDescriptor Descriptor => new(
        Code: "windows-update-catalog",
        DisplayName: "Windows Update Catalog",
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

                if (!TryResolveCatalogRecord(hardwareId, out var match))
                {
                    continue;
                }

                if (bestMatch is null || match.Score > bestMatch.Score)
                {
                    bestMatch = match;
                }
            }

            if (bestMatch is null)
            {
                return Task.FromResult(new ProviderLookupResponse(
                    ProviderCode: Descriptor.Code,
                    IsSuccess: true,
                    Candidates: [],
                    FailureReason: null));
            }

            var candidate = new ProviderCandidate(
                DriverIdentifier: bestMatch.Record.DriverIdentifier,
                CandidateVersion: bestMatch.Record.CandidateVersion,
                ReleaseDateIso: null,
                CompatibilityConfidence: bestMatch.CompatibilityConfidence,
                MatchStrength: bestMatch.MatchStrength,
                ConfidenceRationale: bestMatch.ConfidenceRationale,
                SourceEvidence: new SourceEvidence(
                    bestMatch.Record.SourceUri,
                    bestMatch.Record.PublisherName,
                    SourceTrustLevel.OperatingSystemCatalog,
                    IsOfficialSource: true,
                    bestMatch.EvidenceNote),
                DownloadUri: null);

            return Task.FromResult(new ProviderLookupResponse(
                ProviderCode: Descriptor.Code,
                IsSuccess: true,
                Candidates: [candidate],
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
        string SourceLabel,
        bool IsStaticSnapshot,
        string EvidenceNote);

    internal sealed record SnapshotRecord(
        string HardwareId,
        string DriverIdentifier,
        string CandidateVersion,
        string SourceUri,
        string PublisherName,
        string EvidenceNote);

    private sealed record CatalogMatch(
        CatalogDriverRecord Record,
        HardwareIdMatchStrength MatchStrength,
        CompatibilityConfidence CompatibilityConfidence,
        int Score,
        string ConfidenceRationale,
        string EvidenceNote);

    internal interface IWindowsCatalogDataSource
    {
        string SourceLabel { get; }
        bool IsStaticSnapshot { get; }
        IReadOnlyCollection<SnapshotRecord> LoadRecords();
    }

    internal sealed class JsonFileWindowsCatalogDataSource(string path, string sourceLabel, bool isStaticSnapshot) : IWindowsCatalogDataSource
    {
        private readonly string _path = path;

        public string SourceLabel { get; } = sourceLabel;

        public bool IsStaticSnapshot { get; } = isStaticSnapshot;

        public IReadOnlyCollection<SnapshotRecord> LoadRecords()
        {
            if (string.IsNullOrWhiteSpace(_path) || !File.Exists(_path))
            {
                return [];
            }

            try
            {
                using var stream = File.OpenRead(_path);
                var snapshot = JsonSerializer.Deserialize<List<SnapshotRecord>>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return snapshot ?? [];
            }
            catch (IOException)
            {
                return [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }

    private bool TryResolveCatalogRecord(string hardwareId, out CatalogMatch match)
    {
        var trimmed = hardwareId.Trim();

        if (_catalogByHardwareId.TryGetValue(trimmed, out var exactRecord) && exactRecord is not null)
        {
            match = CreateMatch(exactRecord, HardwareIdMatchStrength.ExactHardwareId, "exact", score: 300);
            return true;
        }

        var normalized = NormalizeHardwareId(trimmed);
        if (normalized is not null &&
            !string.Equals(normalized, trimmed, StringComparison.OrdinalIgnoreCase) &&
            _catalogByHardwareId.TryGetValue(normalized, out var normalizedRecord) &&
            normalizedRecord is not null)
        {
            match = CreateMatch(normalizedRecord, HardwareIdMatchStrength.NormalizedHardwareId, "normalized", score: 200);
            return true;
        }

        if (TryResolveVendorFallback(trimmed, out var fallbackRecord))
        {
            match = CreateMatch(fallbackRecord, HardwareIdMatchStrength.VendorFallback, "vendor-fallback", score: 100);
            return true;
        }

        match = default!;
        return false;
    }

    private static CatalogMatch CreateMatch(
        CatalogDriverRecord record,
        HardwareIdMatchStrength matchStrength,
        string matchLabel,
        int score)
    {
        var confidence = ResolveCompatibilityConfidence(matchStrength, record.IsStaticSnapshot);
        var rationale = BuildConfidenceRationale(matchStrength, record.IsStaticSnapshot, record.SourceLabel);

        var evidenceNote = $"{record.EvidenceNote} Match type: {matchLabel}. Catalog source: {record.SourceLabel}. Confidence rationale: {rationale}";

        return new CatalogMatch(record, matchStrength, confidence, score, rationale, evidenceNote);
    }

    private static CompatibilityConfidence ResolveCompatibilityConfidence(HardwareIdMatchStrength matchStrength, bool isStaticSnapshot)
    {
        var baseConfidence = matchStrength switch
        {
            HardwareIdMatchStrength.ExactHardwareId => CompatibilityConfidence.High,
            HardwareIdMatchStrength.NormalizedHardwareId => CompatibilityConfidence.Medium,
            HardwareIdMatchStrength.VendorFallback => CompatibilityConfidence.Low,
            _ => CompatibilityConfidence.Unknown
        };

        if (!isStaticSnapshot)
        {
            return baseConfidence;
        }

        return baseConfidence switch
        {
            CompatibilityConfidence.High => CompatibilityConfidence.Medium,
            CompatibilityConfidence.Medium => CompatibilityConfidence.Low,
            _ => CompatibilityConfidence.Unknown
        };
    }

    private static string BuildConfidenceRationale(HardwareIdMatchStrength matchStrength, bool isStaticSnapshot, string sourceLabel)
    {
        var matchText = matchStrength switch
        {
            HardwareIdMatchStrength.ExactHardwareId => "hardware id matched exactly",
            HardwareIdMatchStrength.NormalizedHardwareId => "hardware id matched after normalization",
            HardwareIdMatchStrength.VendorFallback => "only vendor-level fallback matched",
            _ => "no deterministic match quality"
        };

        var coverageText = isStaticSnapshot
            ? "source is a static snapshot with partial coverage"
            : "source is updateable and expected to track newer catalog updates";

        return $"{matchText}; {coverageText}; source={sourceLabel}.";
    }

    private static IReadOnlyDictionary<string, CatalogDriverRecord> BuildCatalog(IEnumerable<IWindowsCatalogDataSource> sources)
    {
        var catalog = new Dictionary<string, CatalogDriverRecord>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            var records = source.LoadRecords();
            if (records.Count == 0)
            {
                continue;
            }

            foreach (var entry in records)
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
                    SourceLabel: source.SourceLabel,
                    IsStaticSnapshot: source.IsStaticSnapshot,
                    EvidenceNote: string.IsNullOrWhiteSpace(entry.EvidenceNote)
                        ? "Catalog lookup matched hardware id."
                        : entry.EvidenceNote.Trim());
            }
        }

        return catalog;
    }

    private static IReadOnlyCollection<IWindowsCatalogDataSource> CreateDefaultSources(string? explicitExternalPath = null)
    {
        var sources = new List<IWindowsCatalogDataSource>();

        var externalPath = string.IsNullOrWhiteSpace(explicitExternalPath)
            ? Environment.GetEnvironmentVariable(ExternalCatalogPathEnvironmentVariable)
            : explicitExternalPath;
        if (!string.IsNullOrWhiteSpace(externalPath))
        {
            sources.Add(new JsonFileWindowsCatalogDataSource(
                Path.GetFullPath(externalPath),
                sourceLabel: "external-json",
                isStaticSnapshot: false));
        }

        var snapshotPath = Path.Combine(AppContext.BaseDirectory, SnapshotFileRelativePath);
        sources.Add(new JsonFileWindowsCatalogDataSource(
            snapshotPath,
            sourceLabel: "embedded-snapshot",
            isStaticSnapshot: true));

        return sources;
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

    private bool TryResolveVendorFallback(string hardwareId, out CatalogDriverRecord record)
    {
        record = default!;

        if (!TryExtractVendorToken(hardwareId, out var vendorToken) || string.IsNullOrWhiteSpace(vendorToken))
        {
            return false;
        }

        if (!_catalogByVendorId.TryGetValue(vendorToken, out var vendorRecord) || vendorRecord is null)
        {
            return false;
        }

        record = vendorRecord;
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
