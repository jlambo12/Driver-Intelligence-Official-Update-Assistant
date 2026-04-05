using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

/// <summary>
/// Lightweight OEM support provider that builds an official support-portal handoff URL
/// from known vendor domains and device lookup hints (model/hardware-id).
/// </summary>
public sealed class OfficialOemSupportProviderAdapter : IOfficialProviderAdapter
{
    private static readonly IReadOnlyCollection<OemRule> OemRules =
    [
        new("Dell Support", "https://www.dell.com/support/home/en-us?app=drivers", "dell"),
        new("HP Support", "https://support.hp.com/us-en/drivers", "hp", "hewlett-packard"),
        new("Lenovo Support", "https://pcsupport.lenovo.com/us/en/products", "lenovo"),
        new("ASUS Support", "https://www.asus.com/support/download-center", "asus"),
        new("Acer Support", "https://www.acer.com/us-en/support/drivers-and-manuals", "acer"),
        new("MSI Support", "https://www.msi.com/support", "msi", "micro-star")
    ];

    public ProviderDescriptor Descriptor => new(
        Code: "oem-support-portal",
        DisplayName: "OEM Support Portal",
        IsEnabled: true,
        OfficialSourceOnly: true,
        Precedence: ProviderPrecedence.PrimaryOem);

    public Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var manufacturer = request.DeviceManufacturer?.Trim();
        if (string.IsNullOrWhiteSpace(manufacturer))
        {
            return Task.FromResult(new ProviderLookupResponse(Descriptor.Code, true, [], null));
        }

        var rule = ResolveRule(manufacturer);
        if (rule is null)
        {
            return Task.FromResult(new ProviderLookupResponse(Descriptor.Code, true, [], null));
        }

        var queryHint = ResolveQueryHint(request);
        var sourceUri = BuildLookupUri(rule, queryHint);

        var candidate = new ProviderCandidate(
            DriverIdentifier: $"{Descriptor.Code}:{request.DeviceInstanceId}",
            CandidateVersion: null,
            ReleaseDateIso: null,
            CompatibilityConfidence: CompatibilityConfidence.Unknown,
            MatchStrength: HardwareIdMatchStrength.ManufacturerPortalHint,
            ConfidenceRationale: "OEM support portal links are official but do not identify a specific compatible driver package.",
            SourceEvidence: new SourceEvidence(
                SourceUri: sourceUri,
                PublisherName: rule.PublisherName,
                TrustLevel: SourceTrustLevel.OemSupportPortal,
                IsOfficialSource: true,
                EvidenceNote: BuildEvidenceNote(rule.PublisherName, queryHint)),
            DownloadUri: null);

        return Task.FromResult(new ProviderLookupResponse(
            ProviderCode: Descriptor.Code,
            IsSuccess: true,
            Candidates: [candidate],
            FailureReason: null));
    }

    private static OemRule? ResolveRule(string manufacturer)
    {
        var normalized = manufacturer.Trim().ToLowerInvariant();
        return OemRules.FirstOrDefault(rule => rule.Aliases.Any(alias => normalized.Contains(alias, StringComparison.OrdinalIgnoreCase)));
    }

    private static string? ResolveQueryHint(ProviderLookupRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.DeviceModel))
        {
            return request.DeviceModel.Trim();
        }

        var hardwareId = request.HardwareIds.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        return hardwareId?.Trim();
    }

    private static Uri BuildLookupUri(OemRule rule, string? queryHint)
    {
        if (string.IsNullOrWhiteSpace(queryHint))
        {
            return new Uri(rule.BaseUrl, UriKind.Absolute);
        }

        var separator = rule.BaseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var encoded = Uri.EscapeDataString(queryHint);
        return new Uri($"{rule.BaseUrl}{separator}query={encoded}", UriKind.Absolute);
    }

    private static string BuildEvidenceNote(string publisherName, string? queryHint)
    {
        if (string.IsNullOrWhiteSpace(queryHint))
        {
            return $"Manufacturer mapped to {publisherName} support portal.";
        }

        var displayHint = queryHint.Length > 80 ? $"{queryHint[..80]}…" : queryHint;
        return $"Manufacturer mapped to {publisherName}; support lookup prepared with query hint: {displayHint}.";
    }

    private sealed record OemRule(string PublisherName, string BaseUrl, params string[] Aliases);
}
