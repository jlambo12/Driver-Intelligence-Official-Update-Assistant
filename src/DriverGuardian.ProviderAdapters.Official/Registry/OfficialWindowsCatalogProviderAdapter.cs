using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

/// <summary>
/// Provider adapter backed by a curated Windows Update Catalog snapshot.
/// Coverage is intentionally narrow and returns empty results when no exact hardware-id match exists.
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

                if (!CatalogByHardwareId.TryGetValue(hardwareId.Trim(), out var record))
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
                        record.EvidenceNote),
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
}
