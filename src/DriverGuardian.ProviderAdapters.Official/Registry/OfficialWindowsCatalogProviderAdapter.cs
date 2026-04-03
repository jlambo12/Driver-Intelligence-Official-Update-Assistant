using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

/// <summary>
/// Live provider adapter that queries Microsoft Update Catalog search pages by hardware id.
/// It returns only official-source evidence and never emits download URLs.
/// </summary>
public sealed class OfficialWindowsCatalogProviderAdapter : IOfficialProviderAdapter
{
    private static readonly Regex VersionRegex = new(
        @"\b\d{1,3}(?:\.\d{1,5}){2,3}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly HttpClient _httpClient;

    public OfficialWindowsCatalogProviderAdapter()
        : this(CreateDefaultHttpClient())
    {
    }

    public OfficialWindowsCatalogProviderAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public ProviderDescriptor Descriptor => new(
        Code: "windows-update-catalog",
        DisplayName: "Windows Update Catalog (Live Search)",
        IsEnabled: true,
        OfficialSourceOnly: true,
        Precedence: ProviderPrecedence.PlatformVendor);

    public async Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var hardwareIds = request.HardwareIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeHardwareId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (hardwareIds.Length == 0)
        {
            return new ProviderLookupResponse(
                ProviderCode: Descriptor.Code,
                IsSuccess: false,
                Candidates: [],
                FailureReason: "At least one hardware id is required for provider lookup.");
        }

        foreach (var hardwareId in hardwareIds)
        {
            var searchUri = BuildSearchUri(hardwareId);

            string html;
            try
            {
                html = await _httpClient.GetStringAsync(searchUri, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                return BuildFailure($"Microsoft Update Catalog request failed: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return BuildFailure($"Microsoft Update Catalog request failed: {ex.Message}");
            }

            if (!TryExtractBestVersion(html, out var candidateVersion))
            {
                continue;
            }

            var candidate = new ProviderCandidate(
                DriverIdentifier: $"windows-update-live:{hardwareId}:{candidateVersion}",
                CandidateVersion: candidateVersion,
                ReleaseDateIso: null,
                CompatibilityConfidence: CompatibilityConfidence.Medium,
                SourceEvidence: new SourceEvidence(
                    searchUri,
                    PublisherName: "Microsoft Update Catalog",
                    TrustLevel: SourceTrustLevel.OperatingSystemCatalog,
                    IsOfficialSource: true,
                    EvidenceNote: $"Matched hardware id in live Microsoft Update Catalog search results ({hardwareId})."),
                DownloadUri: null);

            return new ProviderLookupResponse(
                ProviderCode: Descriptor.Code,
                IsSuccess: true,
                Candidates: [candidate],
                FailureReason: null);
        }

        return new ProviderLookupResponse(
            ProviderCode: Descriptor.Code,
            IsSuccess: true,
            Candidates: [],
            FailureReason: null);
    }

    private ProviderLookupResponse BuildFailure(string reason)
        => new(
            ProviderCode: Descriptor.Code,
            IsSuccess: false,
            Candidates: [],
            FailureReason: reason);

    private static HttpClient CreateDefaultHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("DriverGuardian", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("text/html"));

        return client;
    }

    private static Uri BuildSearchUri(string hardwareId)
        => new($"https://www.catalog.update.microsoft.com/Search.aspx?q={Uri.EscapeDataString(hardwareId)}");

    private static string NormalizeHardwareId(string hardwareId)
        => hardwareId.Trim().ToUpperInvariant();

    private static bool TryExtractBestVersion(string html, out string version)
    {
        version = string.Empty;
        if (string.IsNullOrWhiteSpace(html))
        {
            return false;
        }

        var matches = VersionRegex.Matches(WebUtility.HtmlDecode(html));
        if (matches.Count == 0)
        {
            return false;
        }

        var best = matches
            .Select(x => x.Value)
            .Where(v => Version.TryParse(v, out _))
            .OrderByDescending(v => Version.Parse(v))
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(best))
        {
            return false;
        }

        version = best;
        return true;
    }
}
