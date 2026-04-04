using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;
using DriverGuardian.ProviderAdapters.Official.Registry;

namespace DriverGuardian.Tests.Unit.ProviderAdapters.Official;

public sealed class ResilientOfficialProviderAdapterTests
{
    [Fact]
    public async Task LookupAsync_RetriesTransientFailure_AndReturnsSuccess()
    {
        var inner = new ControlledProviderAdapter(new object[]
        {
            new TimeoutException("timeout"),
            CreateSuccessResponse("controlled")
        });

        var adapter = new ResilientOfficialProviderAdapter(
            inner,
            timeout: TimeSpan.FromSeconds(1),
            maxTransientRetries: 1,
            failureThreshold: 3,
            breakDuration: TimeSpan.FromSeconds(10));

        var response = await adapter.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(2, inner.InvocationCount);
    }

    [Fact]
    public async Task LookupAsync_OpensCircuitAfterThreshold_AndShortCircuitsNextCall()
    {
        var inner = new ControlledProviderAdapter(new object[]
        {
            CreateFailedResponse("temporarily unavailable"),
            CreateFailedResponse("temporarily unavailable"),
            CreateFailedResponse("temporarily unavailable"),
            CreateSuccessResponse("controlled")
        });

        var adapter = new ResilientOfficialProviderAdapter(
            inner,
            timeout: TimeSpan.FromSeconds(1),
            maxTransientRetries: 0,
            failureThreshold: 3,
            breakDuration: TimeSpan.FromSeconds(30));

        await adapter.LookupAsync(CreateRequest(), CancellationToken.None);
        await adapter.LookupAsync(CreateRequest(), CancellationToken.None);
        await adapter.LookupAsync(CreateRequest(), CancellationToken.None);
        var shortCircuited = await adapter.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.False(shortCircuited.IsSuccess);
        Assert.Contains("[resilience:circuit_open]", shortCircuited.FailureReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, inner.InvocationCount);
    }

    [Fact]
    public async Task LookupAsync_DoesNotRetry_NonTransientFailureReason()
    {
        var inner = new ControlledProviderAdapter(new object[]
        {
            CreateFailedResponse("invalid request payload")
        });

        var adapter = new ResilientOfficialProviderAdapter(
            inner,
            timeout: TimeSpan.FromSeconds(1),
            maxTransientRetries: 2,
            failureThreshold: 3,
            breakDuration: TimeSpan.FromSeconds(30));

        var response = await adapter.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(1, inner.InvocationCount);
        Assert.Contains("resilience:non_transient_response", response.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_ReportsTransientRetryExhaustionCode()
    {
        var inner = new ControlledProviderAdapter(new object[]
        {
            CreateFailedResponse("temporary timeout"),
            CreateFailedResponse("temporary timeout")
        });

        var adapter = new ResilientOfficialProviderAdapter(
            inner,
            timeout: TimeSpan.FromSeconds(1),
            maxTransientRetries: 1,
            failureThreshold: 3,
            breakDuration: TimeSpan.FromSeconds(30));

        var response = await adapter.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(2, inner.InvocationCount);
        Assert.Contains("resilience:transient_response_retries_exhausted", response.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    private static ProviderLookupRequest CreateRequest()
        => new(
            ProviderCode: "controlled",
            DeviceInstanceId: "DEV-1",
            HardwareIds: ["PCI\\VEN_8086&DEV_15F3"],
            InstalledDriverVersion: "1.0.0",
            OperatingSystemVersion: null,
            DeviceManufacturer: "Test",
            DeviceModel: null);

    private static ProviderLookupResponse CreateSuccessResponse(string code)
        => new(code, true, [], null);

    private static ProviderLookupResponse CreateFailedResponse(string reason)
        => new("controlled", false, [], reason);

    private sealed class ControlledProviderAdapter(IReadOnlyList<object> scriptedResults) : IOfficialProviderAdapter
    {
        private int _cursor;

        public int InvocationCount { get; private set; }

        public ProviderDescriptor Descriptor => new(
            Code: "controlled",
            DisplayName: "Controlled",
            IsEnabled: true,
            OfficialSourceOnly: true,
            Precedence: ProviderPrecedence.PlatformVendor);

        public Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
        {
            InvocationCount++;
            var current = scriptedResults[Math.Min(_cursor, scriptedResults.Count - 1)];
            _cursor++;

            return current switch
            {
                Exception ex => Task.FromException<ProviderLookupResponse>(ex),
                ProviderLookupResponse response => Task.FromResult(response),
                _ => Task.FromResult(CreateFailedResponse("invalid scripted result"))
            };
        }
    }
}
