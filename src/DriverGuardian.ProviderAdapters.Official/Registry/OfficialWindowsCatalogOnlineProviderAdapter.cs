using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

/// <summary>
/// Online adapter that probes Microsoft Update Catalog search and produces
/// official-source candidates based on live HTTP response metadata.
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

        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DriverGuardian/1.0");
        }
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

        var queryHints = BuildQueryHints(request);
        if (queryHints.Count == 0)
        {
            return new ProviderLookupResponse(Descriptor.Code, true, [], null);
        }

        var failures = new List<string>();

        foreach (var hint in queryHints)
        {
            var searchUri = BuildSearchUri(hint.Query);
            using var response = await _httpClient.GetAsync(searchUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                failures.Add(BuildHttpFailure(searchUri, response.StatusCode));
                continue;
            }

            var htmlBody = await TryReadHtmlBodyAsync(response, cancellationToken);
            var insights = ExtractInsights(htmlBody);
            if (insights.IsNoResults)
            {
                continue;
            }

            var candidate = BuildCandidate(request, hint, searchUri, response.StatusCode, insights);

            return new ProviderLookupResponse(
                ProviderCode: Descriptor.Code,
                IsSuccess: true,
                Candidates: [candidate],
                FailureReason: null);
        }

        if (failures.Count > 0)
        {
            return new ProviderLookupResponse(
                ProviderCode: Descriptor.Code,
                IsSuccess: false,
                Candidates: [],
                FailureReason: string.Join(" | ", failures));
        }

        return new ProviderLookupResponse(
            ProviderCode: Descriptor.Code,
            IsSuccess: true,
            Candidates: [],
            FailureReason: null);
    }

    private ProviderCandidate BuildCandidate(
        ProviderLookupRequest request,
        QueryHint hint,
        Uri searchUri,
        HttpStatusCode statusCode,
        CatalogInsights insights)
    {
        var confidence = hint.MatchStrength switch
        {
            HardwareIdMatchStrength.ExactHardwareId => CompatibilityConfidence.High,
            HardwareIdMatchStrength.NormalizedHardwareId => CompatibilityConfidence.Medium,
            HardwareIdMatchStrength.VendorFallback => CompatibilityConfidence.Low,
            _ => CompatibilityConfidence.Low
        };

        return new ProviderCandidate(
            DriverIdentifier: $"{Descriptor.Code}:{request.DeviceInstanceId}:{hint.Kind}",
            CandidateVersion: insights.CandidateVersion,
            ReleaseDateIso: null,
            CompatibilityConfidence: confidence,
            MatchStrength: hint.MatchStrength,
            ConfidenceRationale: $"Live catalog endpoint returned result page using {hint.Kind} query hint; final package fit must be user-validated.",
            SourceEvidence: new SourceEvidence(
                SourceUri: searchUri,
                PublisherName: "Microsoft Update Catalog",
                TrustLevel: SourceTrustLevel.OperatingSystemCatalog,
                IsOfficialSource: true,
                EvidenceNote: BuildEvidenceNote(hint.Query, hint.Kind, insights, statusCode)),
            DownloadUri: null);
    }

    private static string BuildHttpFailure(Uri searchUri, HttpStatusCode statusCode)
    {
        var isTransientStatus = (int)statusCode is 408 or 429 or >= 500;
        var transientText = isTransientStatus ? "temporary-unavailable" : "non-transient-http";
        return $"[{transientText}] Catalog online request failed for {searchUri} with {(int)statusCode} ({statusCode}).";
    }

    private static IReadOnlyList<QueryHint> BuildQueryHints(ProviderLookupRequest request)
    {
        var hints = new List<QueryHint>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var hardwareIdRaw in request.HardwareIds)
        {
            if (string.IsNullOrWhiteSpace(hardwareIdRaw))
            {
                continue;
            }

            var hardwareId = hardwareIdRaw.Trim();
            AddHint(hints, seen, hardwareId, "exact-hardware-id", HardwareIdMatchStrength.ExactHardwareId);

            var normalized = NormalizeHardwareId(hardwareId);
            if (!string.IsNullOrWhiteSpace(normalized) && !string.Equals(normalized, hardwareId, StringComparison.OrdinalIgnoreCase))
            {
                AddHint(hints, seen, normalized, "normalized-hardware-id", HardwareIdMatchStrength.NormalizedHardwareId);
            }

            var vendorToken = ResolveVendorToken(hardwareId);
            if (!string.IsNullOrWhiteSpace(vendorToken))
            {
                AddHint(hints, seen, vendorToken, "vendor-fallback", HardwareIdMatchStrength.VendorFallback);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceModel))
        {
            AddHint(hints, seen, request.DeviceModel.Trim(), "device-model", HardwareIdMatchStrength.VendorFallback);
        }

        return hints;
    }

    private static void AddHint(List<QueryHint> hints, HashSet<string> seen, string query, string kind, HardwareIdMatchStrength matchStrength)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        if (!seen.Add(query))
        {
            return;
        }

        hints.Add(new QueryHint(query, kind, matchStrength));
    }

    private static string? NormalizeHardwareId(string hardwareId)
    {
        var trimmed = hardwareId.Trim();
        var index = trimmed.IndexOf("&SUBSYS", StringComparison.OrdinalIgnoreCase);
        if (index > 0)
        {
            trimmed = trimmed[..index];
        }

        index = trimmed.IndexOf("&REV", StringComparison.OrdinalIgnoreCase);
        if (index > 0)
        {
            trimmed = trimmed[..index];
        }

        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string? ResolveVendorToken(string hardwareId)
    {
        var upper = hardwareId.ToUpperInvariant();
        var venIndex = upper.IndexOf("VEN_", StringComparison.Ordinal);
        if (venIndex >= 0 && upper.Length >= venIndex + 8)
        {
            return upper.Substring(venIndex, 8);
        }

        var vidIndex = upper.IndexOf("VID_", StringComparison.Ordinal);
        if (vidIndex >= 0 && upper.Length >= vidIndex + 8)
        {
            return upper.Substring(vidIndex, 8);
        }

        return null;
    }

    private static Uri BuildSearchUri(string query)
    {
        var encoded = Uri.EscapeDataString(query);
        return new Uri($"{CatalogSearchBaseUri}?q={encoded}", UriKind.Absolute);
    }

    private static async Task<string> TryReadHtmlBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType is not null && !contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(body) ? string.Empty : body;
    }

    private static CatalogInsights ExtractInsights(string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(htmlBody))
        {
            return new CatalogInsights(false, null, null, []);
        }

        var title = ExtractHtmlTitle(htmlBody);
        var isNoResults = htmlBody.Contains("did not find any results", StringComparison.OrdinalIgnoreCase)
                          || htmlBody.Contains("no results", StringComparison.OrdinalIgnoreCase);

        var topResultTitles = Regex.Matches(htmlBody, @">([^<]{12,160})<")
            .Select(match => WebUtility.HtmlDecode(match.Groups[1].Value.Trim()))
            .Where(text => text.Contains("KB", StringComparison.OrdinalIgnoreCase) || text.Contains("driver", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();

        var kbMatch = Regex.Match(htmlBody, @"\bKB\d{6,8}\b", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
        var versionMatch = Regex.Match(htmlBody, @"\b\d+\.\d+(?:\.\d+){1,3}\b", RegexOptions.None, TimeSpan.FromMilliseconds(200));

        var evidenceTitle = !string.IsNullOrWhiteSpace(title)
            ? title
            : topResultTitles.FirstOrDefault();

        return new CatalogInsights(
            IsNoResults: isNoResults,
            PageTitle: evidenceTitle,
            CandidateVersion: versionMatch.Success ? versionMatch.Value : null,
            TopResultTitles: topResultTitles.Length == 0
                ? (kbMatch.Success ? [$"Detected {kbMatch.Value}"] : [])
                : topResultTitles);
    }

    private static string? ExtractHtmlTitle(string htmlBody)
    {
        var match = Regex.Match(htmlBody, "<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromMilliseconds(200));
        if (!match.Success)
        {
            return null;
        }

        var title = WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
        return string.IsNullOrWhiteSpace(title) ? null : title;
    }

    private static string BuildEvidenceNote(string query, string kind, CatalogInsights insights, HttpStatusCode statusCode)
    {
        var queryDisplay = query.Length <= 80 ? query : $"{query[..80]}…";
        var statusPart = $"Live catalog check succeeded with status {(int)statusCode} ({statusCode}) using {kind}.";
        var titlePart = string.IsNullOrWhiteSpace(insights.PageTitle)
            ? ""
            : $" Page title: {insights.PageTitle}.";
        var topResultsPart = insights.TopResultTitles.Count == 0
            ? ""
            : $" Top signals: {string.Join(" | ", insights.TopResultTitles)}.";

        return $"{statusPart} Query: {queryDisplay}.{titlePart}{topResultsPart}";
    }

    private sealed record QueryHint(string Query, string Kind, HardwareIdMatchStrength MatchStrength);

    private sealed record CatalogInsights(
        bool IsNoResults,
        string? PageTitle,
        string? CandidateVersion,
        IReadOnlyList<string> TopResultTitles);
}
