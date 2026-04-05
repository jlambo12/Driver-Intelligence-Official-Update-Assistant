using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

/// <summary>
/// Online adapter that probes Microsoft Update Catalog search and produces
/// an official-source handoff candidate based on live HTTP response metadata.
/// </summary>
public sealed class OfficialWindowsCatalogOnlineProviderAdapter : IOfficialProviderAdapter
{
    private static readonly Uri CatalogSearchBaseUri = new("https://www.catalog.update.microsoft.com/Search.aspx", UriKind.Absolute);
    private readonly HttpClient _httpClient;

    public OfficialWindowsCatalogOnlineProviderAdapter()
        : this(new HttpClient())
    {
    }

    public OfficialWindowsCatalogOnlineProviderAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public ProviderDescriptor Descriptor => new(
        Code: "windows-update-catalog-online",
        DisplayName: "Windows Update Catalog (Online)",
        IsEnabled: true,
        OfficialSourceOnly: true,
        Precedence: ProviderPrecedence.PlatformVendor);

    public async Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var query = ResolveSearchQuery(request);
        if (query is null)
        {
            return new ProviderLookupResponse(Descriptor.Code, true, [], null);
        }

        var searchUri = BuildSearchUri(query);
        using var response = await _httpClient.GetAsync(searchUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ProviderLookupResponse(
                ProviderCode: Descriptor.Code,
                IsSuccess: false,
                Candidates: [],
                FailureReason: $"Catalog online request returned {(int)response.StatusCode} ({response.StatusCode}).");
        }

        var title = await TryReadHtmlTitleAsync(response, cancellationToken);
        var evidenceNote = BuildEvidenceNote(query, title, response.StatusCode);

        var candidate = new ProviderCandidate(
            DriverIdentifier: $"{Descriptor.Code}:{request.DeviceInstanceId}",
            CandidateVersion: null,
            ReleaseDateIso: null,
            CompatibilityConfidence: CompatibilityConfidence.Medium,
            MatchStrength: HardwareIdMatchStrength.NormalizedHardwareId,
            ConfidenceRationale: "Live catalog endpoint responded successfully for the device query; package compatibility must still be user-validated.",
            SourceEvidence: new SourceEvidence(
                SourceUri: searchUri,
                PublisherName: "Microsoft Update Catalog",
                TrustLevel: SourceTrustLevel.OperatingSystemCatalog,
                IsOfficialSource: true,
                EvidenceNote: evidenceNote),
            DownloadUri: null);

        return new ProviderLookupResponse(
            ProviderCode: Descriptor.Code,
            IsSuccess: true,
            Candidates: [candidate],
            FailureReason: null);
    }

    private static string? ResolveSearchQuery(ProviderLookupRequest request)
    {
        foreach (var hardwareId in request.HardwareIds)
        {
            if (!string.IsNullOrWhiteSpace(hardwareId))
            {
                return hardwareId.Trim();
            }
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceModel))
        {
            return request.DeviceModel.Trim();
        }

        return null;
    }

    private static Uri BuildSearchUri(string query)
    {
        var encoded = Uri.EscapeDataString(query);
        return new Uri($"{CatalogSearchBaseUri}?q={encoded}", UriKind.Absolute);
    }

    private static async Task<string?> TryReadHtmlTitleAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType is not null && !contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        var match = Regex.Match(body, "<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromMilliseconds(250));
        if (!match.Success)
        {
            return null;
        }

        var title = WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
        return string.IsNullOrWhiteSpace(title) ? null : title;
    }

    private static string BuildEvidenceNote(string query, string? title, HttpStatusCode statusCode)
    {
        var queryDisplay = query.Length <= 80 ? query : $"{query[..80]}…";
        var statusPart = $"Live catalog check succeeded with status {(int)statusCode} ({statusCode}).";
        var titlePart = string.IsNullOrWhiteSpace(title)
            ? ""
            : $" Page title: {title}.";

        return $"{statusPart} Query: {queryDisplay}.{titlePart}";
    }
}
