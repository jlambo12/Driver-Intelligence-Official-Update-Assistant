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
    private readonly int _circuitBreakFailureThreshold;
    private readonly TimeSpan _circuitBreakDuration;
    private readonly Func<DateTimeOffset> _utcNow;

    private readonly object _resilienceStateLock = new();
    private int _consecutiveTransientFailures;
    private DateTimeOffset? _circuitOpenedAtUtc;

    public OfficialMicrosoftSupportOnlineProviderAdapter()
        : this(new HttpClient())
    {
    }

    public OfficialMicrosoftSupportOnlineProviderAdapter(HttpClient httpClient)
        : this(
            httpClient,
            requestTimeout: TimeSpan.FromSeconds(4),
            maxAttempts: 3,
            baseRetryDelay: TimeSpan.FromMilliseconds(120),
            circuitBreakFailureThreshold: 3,
            circuitBreakDuration: TimeSpan.FromSeconds(30),
            utcNow: () => DateTimeOffset.UtcNow)
    {
    }

    internal OfficialMicrosoftSupportOnlineProviderAdapter(
        HttpClient httpClient,
        TimeSpan requestTimeout,
        int maxAttempts,
        TimeSpan baseRetryDelay,
        int circuitBreakFailureThreshold,
        TimeSpan circuitBreakDuration,
        Func<DateTimeOffset> utcNow)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _requestTimeout = requestTimeout <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(requestTimeout)) : requestTimeout;
        _maxAttempts = maxAttempts <= 0 ? throw new ArgumentOutOfRangeException(nameof(maxAttempts)) : maxAttempts;
        _baseRetryDelay = baseRetryDelay < TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(baseRetryDelay)) : baseRetryDelay;
        _circuitBreakFailureThreshold = circuitBreakFailureThreshold <= 0
            ? throw new ArgumentOutOfRangeException(nameof(circuitBreakFailureThreshold))
            : circuitBreakFailureThreshold;
        _circuitBreakDuration = circuitBreakDuration <= TimeSpan.Zero
            ? throw new ArgumentOutOfRangeException(nameof(circuitBreakDuration))
            : circuitBreakDuration;
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));

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

        if (IsCircuitOpen(out var openedAgo))
        {
            return new ProviderLookupResponse(
                ProviderCode: Descriptor.Code,
                IsSuccess: false,
                Candidates: [],
                FailureReason: $"[temporary-unavailable] Microsoft Support circuit is open after repeated transient failures ({openedAgo.TotalSeconds:N0}s elapsed). Try again later.");
        }

        var failures = new List<string>();

        foreach (var hint in queryHints)
        {
            if (IsCircuitOpen(out var openedAgoDuringLookup))
            {
                failures.Add($"[temporary-unavailable] Microsoft Support circuit is open after repeated transient failures ({openedAgoDuringLookup.TotalSeconds:N0}s elapsed). Try again later.");
                break;
            }

            var searchUri = BuildSearchUri(hint.Query);
            var probe = await ProbeWithRetryAsync(searchUri, cancellationToken);

            if (!probe.IsSuccess)
            {
                if (!string.IsNullOrWhiteSpace(probe.FailureReason))
                {
                    failures.Add(probe.FailureReason);
                }

                continue;
            }

            RegisterSuccess();

            var candidate = new ProviderCandidate(
                DriverIdentifier: $"{Descriptor.Code}:{request.DeviceInstanceId}",
                CandidateVersion: null,
                ReleaseDateIso: null,
                CompatibilityConfidence: ResolveConfidence(hint.Kind),
                MatchStrength: ResolveMatchStrength(hint.Kind),
                ConfidenceRationale: BuildConfidenceRationale(hint.Kind),
                SourceEvidence: new SourceEvidence(
                    SourceUri: searchUri,
                    PublisherName: "Microsoft Support",
                    TrustLevel: SourceTrustLevel.OfficialPublisherSite,
                    IsOfficialSource: true,
                    EvidenceNote: $"Live Microsoft Support search endpoint validated for {hint.Kind} query hint '{TrimForEvidence(hint.Query)}'."),
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
                        RegisterTransientFailure();
                        await DelayForRetryAsync(attempt, cancellationToken);
                        continue;
                    }

                    if (IsTransient(response.StatusCode))
                    {
                        RegisterTransientFailure();
                    }

                    return new ProbeResult(false, string.Join(" ; ", failures));
                }

                return new ProbeResult(true, null);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                var failure = $"[temporary-unavailable] Microsoft Support request timed out for {searchUri}.";
                failures.Add($"attempt {attempt}/{_maxAttempts}: {failure}");
                RegisterTransientFailure();

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
                RegisterTransientFailure();

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

    private bool IsCircuitOpen(out TimeSpan openedAgo)
    {
        lock (_resilienceStateLock)
        {
            var now = _utcNow();

            if (_circuitOpenedAtUtc is null)
            {
                openedAgo = TimeSpan.Zero;
                return false;
            }

            openedAgo = now - _circuitOpenedAtUtc.Value;
            if (openedAgo >= _circuitBreakDuration)
            {
                _circuitOpenedAtUtc = null;
                _consecutiveTransientFailures = 0;
                openedAgo = TimeSpan.Zero;
                return false;
            }

            return true;
        }
    }

    private void RegisterTransientFailure()
    {
        lock (_resilienceStateLock)
        {
            _consecutiveTransientFailures++;

            if (_consecutiveTransientFailures >= _circuitBreakFailureThreshold)
            {
                _circuitOpenedAtUtc = _utcNow();
            }
        }
    }

    private void RegisterSuccess()
    {
        lock (_resilienceStateLock)
        {
            _consecutiveTransientFailures = 0;
            _circuitOpenedAtUtc = null;
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    private static List<QueryHint> BuildQueryHints(ProviderLookupRequest request)
    {
        var hints = new List<QueryHint>();

        foreach (var hardwareId in request.HardwareIds)
        {
            if (!string.IsNullOrWhiteSpace(hardwareId))
            {
                hints.Add(new QueryHint(hardwareId.Trim(), QueryHintKind.HardwareId));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceModel))
        {
            hints.Add(new QueryHint(request.DeviceModel.Trim(), QueryHintKind.DeviceModel));
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceManufacturer))
        {
            hints.Add(new QueryHint(request.DeviceManufacturer.Trim(), QueryHintKind.Manufacturer));
        }

        return hints
            .DistinctBy(x => x.Query, StringComparer.OrdinalIgnoreCase)
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

    private static CompatibilityConfidence ResolveConfidence(QueryHintKind kind)
        => kind switch
        {
            QueryHintKind.HardwareId => CompatibilityConfidence.Medium,
            QueryHintKind.DeviceModel => CompatibilityConfidence.Low,
            _ => CompatibilityConfidence.Unknown
        };

    private static HardwareIdMatchStrength ResolveMatchStrength(QueryHintKind kind)
        => kind switch
        {
            QueryHintKind.HardwareId => HardwareIdMatchStrength.NormalizedHardwareId,
            _ => HardwareIdMatchStrength.ManufacturerPortalHint
        };

    private static string BuildConfidenceRationale(QueryHintKind kind)
        => kind switch
        {
            QueryHintKind.HardwareId => "Microsoft Support search validated with hardware-id hint; manual package compatibility confirmation is still required.",
            QueryHintKind.DeviceModel => "Microsoft Support search validated with device-model hint; result is an official discovery path, not a guaranteed package match.",
            _ => "Microsoft Support search validated with manufacturer hint; this is an official handoff path requiring manual driver validation."
        };

    private sealed record ProbeResult(bool IsSuccess, string? FailureReason);
    private sealed record QueryHint(string Query, QueryHintKind Kind);

    private enum QueryHintKind
    {
        HardwareId,
        DeviceModel,
        Manufacturer
    }
}
