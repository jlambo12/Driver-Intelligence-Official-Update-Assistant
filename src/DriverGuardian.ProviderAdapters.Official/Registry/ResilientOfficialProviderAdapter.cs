using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;
using System.Net.Http;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

/// <summary>
/// Adds timeout/retry/circuit-breaker behavior around provider lookups.
/// </summary>
public sealed class ResilientOfficialProviderAdapter(
    IOfficialProviderAdapter inner,
    TimeSpan timeout,
    int maxTransientRetries,
    int failureThreshold,
    TimeSpan breakDuration) : IOfficialProviderAdapter
{
    private readonly IOfficialProviderAdapter _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly TimeSpan _timeout = timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(3) : timeout;
    private readonly int _maxTransientRetries = Math.Max(0, maxTransientRetries);
    private readonly int _failureThreshold = Math.Max(1, failureThreshold);
    private readonly TimeSpan _breakDuration = breakDuration <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : breakDuration;
    private readonly object _stateGate = new();
    private int _consecutiveFailures;
    private DateTimeOffset? _openUntilUtc;

    public IOfficialProviderAdapter InnerProvider => _inner;

    public ProviderDescriptor Descriptor => _inner.Descriptor;

    public async Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
    {
        if (IsCircuitOpen())
        {
            return new ProviderLookupResponse(
                ProviderCode: Descriptor.Code,
                IsSuccess: false,
                Candidates: [],
                FailureReason: "[resilience:circuit_open] Provider circuit is temporarily open due to repeated failures.");
        }

        ProviderLookupResponse? lastResponse = null;
        Exception? lastException = null;
        string? failureCode = null;
        var attempts = _maxTransientRetries + 1;
        var attemptedCalls = 0;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);
            attemptedCalls++;

            try
            {
                var response = await _inner.LookupAsync(request, timeoutCts.Token);
                if (response.IsSuccess)
                {
                    ResetCircuitState();
                    return response;
                }

                lastResponse = response;
                var isTransient = IsTransientFailureReason(response.FailureReason);
                if (!isTransient)
                {
                    failureCode = "non_transient_response";
                    break;
                }

                if (attempt == attempts)
                {
                    failureCode = "transient_response_retries_exhausted";
                    break;
                }

                await Task.Delay(ComputeBackoff(attempt), cancellationToken);
            }
            catch (Exception ex) when (IsTransientException(ex))
            {
                lastException = ex;
                if (attempt == attempts)
                {
                    failureCode = "transient_exception_retries_exhausted";
                    break;
                }

                await Task.Delay(ComputeBackoff(attempt), cancellationToken);
            }
        }

        RegisterFailure();
        if (lastResponse is not null)
        {
            return new ProviderLookupResponse(
                ProviderCode: lastResponse.ProviderCode,
                IsSuccess: false,
                Candidates: lastResponse.Candidates,
                FailureReason: $"[resilience:{failureCode ?? "response_failure"};attempts={attemptedCalls}] {lastResponse.FailureReason ?? "Unknown provider failure."}");
        }

        return new ProviderLookupResponse(
            ProviderCode: Descriptor.Code,
            IsSuccess: false,
            Candidates: [],
            FailureReason: $"[resilience:{failureCode ?? "exception_failure"};attempts={attemptedCalls}] Provider lookup failed after retries: {lastException?.Message ?? "Unknown failure."}");
    }

    private bool IsCircuitOpen()
    {
        lock (_stateGate)
        {
            var openUntil = _openUntilUtc;
            if (openUntil is null)
            {
                return false;
            }

            if (DateTimeOffset.UtcNow < openUntil.Value)
            {
                return true;
            }

            _openUntilUtc = null;
            _consecutiveFailures = 0;
            return false;
        }
    }

    private void ResetCircuitState()
    {
        lock (_stateGate)
        {
            _consecutiveFailures = 0;
            _openUntilUtc = null;
        }
    }

    private void RegisterFailure()
    {
        lock (_stateGate)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= _failureThreshold)
            {
                _openUntilUtc = DateTimeOffset.UtcNow.Add(_breakDuration);
            }
        }
    }

    private static bool IsTransientException(Exception ex)
        => ex is TimeoutException
            or TaskCanceledException
            or OperationCanceledException
            or HttpRequestException;

    private static bool IsTransientFailureReason(string? failureReason)
    {
        if (string.IsNullOrWhiteSpace(failureReason))
        {
            return false;
        }

        return failureReason.Contains("timeout", StringComparison.OrdinalIgnoreCase)
               || failureReason.Contains("temporar", StringComparison.OrdinalIgnoreCase)
               || failureReason.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
               || failureReason.Contains("unavailable", StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan ComputeBackoff(int attempt)
        => TimeSpan.FromMilliseconds(Math.Min(250 * attempt, 1000));
}
