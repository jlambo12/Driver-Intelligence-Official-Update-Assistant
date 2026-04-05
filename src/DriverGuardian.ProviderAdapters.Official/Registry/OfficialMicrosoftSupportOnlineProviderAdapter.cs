using System.Net;
using System.Net.Http;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

/// <summary>
/// Online adapter that prepares a Microsoft Support search handoff and validates
/// endpoint availability with bounded retry/timeout policy.
/// </summary>
public sealed class OfficialMicrosoftSupportOnlineProviderAdapter : IOfficialProviderAdapter
{
    private static readonly Uri SupportSearchBaseUri = new("https://support.microsoft.com/search", UriKind.Absolute);

    private readonly HttpClient _httpClient;
    private readonly TimeSpan _requestTimeout;
    private readonly TimeSpan _baseRetryDelay;
    private readonly int _maxAttempts;

    public OfficialMicrosoftSupportOnlineProviderAdapter()
        : this(new HttpClient())
    {
    }

    public OfficialMicrosoftSupportOnlineProviderAdapter(HttpClient httpClient)
        : this(
            httpClient,
            requestTimeout: TimeSpan.FromSeconds(4),
            maxAttempts: 3,
            baseRetryDelay: TimeSpan.FromMilliseconds(120))
    {
    }

    internal OfficialMicrosoftSupportOnlineProviderAdapter(
        HttpClient httpClient,
        TimeSpan requestTimeout,
        int maxAttempts,
        TimeSpan baseRetryDelay)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _requestTimeout = requestTimeout <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(requestTimeout)) : requestTimeout;
        _maxAttempts = maxAttempts <= 0 ? throw new ArgumentOutOfRangeException(nameof(maxAttempts)) : maxAttempts;
        _baseRetryDelay = baseRetryDelay < TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(baseRetryDelay)) : baseRetryDelay;

        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DriverGuardian/1.0");
        }
    }

    public ProviderDescriptor Descriptor => new(
        Code: "microsoft-support-online",
        DisplayName: "Microsoft Support (Online)",
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
            var searchUri = BuildSearchUri(hint);
            var probe = await ProbeWithRetryAsync(searchUri, cancellationToken);

            if (!probe.IsSuccess)
            {
                if (!string.IsNullOrWhiteSpace(probe.FailureReason))
                {
                    failures.Add(probe.FailureReason);
                }

                continue;
            }

            var candidate = new ProviderCandidate(
                DriverIdentifier: $"{Descriptor.Code}:{request.DeviceInstanceId}",
                CandidateVersion: null,
                ReleaseDateIso: null,
                CompatibilityConfidence: CompatibilityConfidence.Medium,
                MatchStrength: HardwareIdMatchStrength.ManufacturerPortalHint,
                ConfidenceRationale: "Microsoft Support search provides an official handoff path but requires manual driver package validation.",
                SourceEvidence: new SourceEvidence(
                    SourceUri: searchUri,
                    PublisherName: "Microsoft Support",
                    TrustLevel: SourceTrustLevel.OfficialPublisherSite,
                    IsOfficialSource: true,
                    EvidenceNote: $"Live Microsoft Support search endpoint validated for query hint '{TrimForEvidence(hint)}'."),
                DownloadUri: null);

            return new ProviderLookupResponse(
                ProviderCode: Descriptor.Code,
                IsSuccess: true,
                Candidates: [candidate],
                FailureReason: null);
        }

        return failures.Count == 0
            ? new ProviderLookupResponse(Descriptor.Code, true, [], null)
            : new ProviderLookupResponse(Descriptor.Code, false, [], string.Join(" | ", failures));
    }

    private async Task<ProbeResult> ProbeWithRetryAsync(Uri searchUri, CancellationToken cancellationToken)
    {
        var failures = new List<string>();

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_requestTimeout);

                using var response = await _httpClient.GetAsync(searchUri, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var failure = $"[temporary-unavailable] Microsoft Support returned HTTP {(int)response.StatusCode} for {searchUri}.";
                    failures.Add($"attempt {attempt}/{_maxAttempts}: {failure}");

                    if (IsTransient(response.StatusCode) && attempt < _maxAttempts)
                    {
                        await DelayForRetryAsync(attempt, cancellationToken);
                        continue;
                    }

                    return new ProbeResult(false, string.Join(" ; ", failures));
                }

                return new ProbeResult(true, null);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                var failure = $"[temporary-unavailable] Microsoft Support request timed out for {searchUri}.";
                failures.Add($"attempt {attempt}/{_maxAttempts}: {failure}");

                if (attempt < _maxAttempts)
                {
                    await DelayForRetryAsync(attempt, cancellationToken);
                    continue;
                }

                return new ProbeResult(false, string.Join(" ; ", failures));
            }
            catch (HttpRequestException ex)
            {
                var failure = $"[temporary-unavailable] Microsoft Support network failure for {searchUri}: {ex.Message}";
                failures.Add($"attempt {attempt}/{_maxAttempts}: {failure}");

                if (attempt < _maxAttempts)
                {
                    await DelayForRetryAsync(attempt, cancellationToken);
                    continue;
                }

                return new ProbeResult(false, string.Join(" ; ", failures));
            }
        }

        return new ProbeResult(false, string.Join(" ; ", failures));
    }

    private async Task DelayForRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        if (_baseRetryDelay <= TimeSpan.Zero)
        {
            return;
        }

        var factor = Math.Pow(2, Math.Max(0, attempt - 1));
        var delay = TimeSpan.FromMilliseconds(_baseRetryDelay.TotalMilliseconds * factor);
        await Task.Delay(delay, cancellationToken);
    }

    private static bool IsTransient(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    private static List<string> BuildQueryHints(ProviderLookupRequest request)
    {
        var hints = new List<string>();

        foreach (var hardwareId in request.HardwareIds)
        {
            if (!string.IsNullOrWhiteSpace(hardwareId))
            {
                hints.Add(hardwareId.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceModel))
        {
            hints.Add(request.DeviceModel.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceManufacturer))
        {
            hints.Add(request.DeviceManufacturer.Trim());
        }

        return hints
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
    }

    private static Uri BuildSearchUri(string queryHint)
    {
        var encoded = Uri.EscapeDataString(queryHint);
        return new Uri($"{SupportSearchBaseUri}?query={encoded}", UriKind.Absolute);
    }

    private static string TrimForEvidence(string queryHint)
        => queryHint.Length <= 80 ? queryHint : $"{queryHint[..80]}…";

    private sealed record ProbeResult(bool IsSuccess, string? FailureReason);
}
