using System.Net;
using System.Net.Http;
using System.Text;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Official.Registry;

namespace DriverGuardian.Tests.Unit.ProviderAdapters.Official;

public sealed class OfficialWindowsCatalogOnlineProviderAdapterTests
{
    [Fact]
    public async Task LookupAsync_ReturnsCandidate_WhenCatalogRespondsSuccessfully()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html><head><title>Microsoft Update Catalog</title></head><body>Driver KB5021234 Version 10.2.3.4</body></html>", Encoding.UTF8, "text/html")
        });

        var adapter = new OfficialWindowsCatalogOnlineProviderAdapter(new HttpClient(handler));

        var response = await adapter.LookupAsync(CreateRequest("PCI\\VEN_8086&DEV_A2AF", "FallbackModelX"), CancellationToken.None);

        Assert.True(response.IsSuccess);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal(SourceTrustLevel.OperatingSystemCatalog, candidate.SourceEvidence.TrustLevel);
        Assert.Contains("catalog.update.microsoft.com", candidate.SourceEvidence.SourceUri.Host, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PCI%5CVEN_8086%26DEV_A2AF", candidate.SourceEvidence.SourceUri.Query, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("10.2.3.4", candidate.CandidateVersion);
        Assert.Contains("Microsoft Update Catalog", candidate.SourceEvidence.EvidenceNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_TriesFallbackHint_WhenExactQueryHasNoResults()
    {
        var calls = new List<string>();
        var handler = new StubHttpMessageHandler(request =>
        {
            calls.Add(request.RequestUri!.Query);

            var query = request.RequestUri!.Query;
            if (query.Contains("SUBSYS_12345678", StringComparison.OrdinalIgnoreCase)
                || query.Contains("SUBSYS%5F12345678", StringComparison.OrdinalIgnoreCase)
                || query.Contains("SUBSYS%5f12345678", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<html><body>We did not find any results for your search</body></html>", Encoding.UTF8, "text/html")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html><head><title>Microsoft Update Catalog</title></head><body>Driver results found</body></html>", Encoding.UTF8, "text/html")
            };
        });

        var adapter = new OfficialWindowsCatalogOnlineProviderAdapter(new HttpClient(handler));

        var response = await adapter.LookupAsync(CreateRequest("PCI\\VEN_8086&DEV_0000&SUBSYS_12345678", "FallbackModelX"), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.NotEmpty(calls);
        Assert.True(calls.Count >= 2);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal(HardwareIdMatchStrength.NormalizedHardwareId, candidate.MatchStrength);
        Assert.Contains("normalized-hardware-id", candidate.ConfidenceRationale, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_RetriesTransientStatus_AndSucceedsWithinRetryWindow()
    {
        var callCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            callCount++;

            if (callCount <= 2)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("maintenance", Encoding.UTF8, "text/plain")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html><head><title>Microsoft Update Catalog</title></head><body>Driver Version 11.0.0.1</body></html>", Encoding.UTF8, "text/html")
            };
        });

        var adapter = new OfficialWindowsCatalogOnlineProviderAdapter(new HttpClient(handler));

        var response = await adapter.LookupAsync(CreateRequest("PCI\\VEN_10EC&DEV_8168", "FallbackModelX"), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(3, callCount);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal("11.0.0.1", candidate.CandidateVersion);
    }

    [Fact]
    public async Task LookupAsync_OpensCircuitBreaker_AfterRepeatedTransientFailures()
    {
        var callCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("maintenance", Encoding.UTF8, "text/plain")
            };
        });

        var adapter = new OfficialWindowsCatalogOnlineProviderAdapter(new HttpClient(handler));

        var first = await adapter.LookupAsync(CreateRequest("PCI\\VEN_10EC&DEV_8168", "FallbackModelX"), CancellationToken.None);
        Assert.False(first.IsSuccess);
        Assert.Equal(3, callCount);

        var second = await adapter.LookupAsync(CreateRequest("PCI\\VEN_8086&DEV_A2AF", "FallbackModelX"), CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Contains("circuit is open", second.FailureReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task LookupAsync_ReturnsFailure_WhenCatalogReturnsServerError()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("maintenance", Encoding.UTF8, "text/plain")
        });

        var adapter = new OfficialWindowsCatalogOnlineProviderAdapter(new HttpClient(handler));

        var response = await adapter.LookupAsync(CreateRequest("PCI\\VEN_10EC&DEV_8168", "FallbackModelX"), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Empty(response.Candidates);
        Assert.Contains("temporary-unavailable", response.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_ReturnsEmpty_WhenNoSearchHintProvided()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not hit network without query."));
        var adapter = new OfficialWindowsCatalogOnlineProviderAdapter(new HttpClient(handler));

        var response = await adapter.LookupAsync(CreateRequest(null, null), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Empty(response.Candidates);
    }

    private static ProviderLookupRequest CreateRequest(string? hardwareId, string? deviceModel)
    {
        var hardwareIds = string.IsNullOrWhiteSpace(hardwareId)
            ? Array.Empty<string>()
            : [hardwareId];

        return new ProviderLookupRequest(
            ProviderCode: "windows-update-catalog-online",
            DeviceInstanceId: "DEV-ONLINE-1",
            HardwareIds: hardwareIds,
            InstalledDriverVersion: "1.0.0",
            OperatingSystemVersion: "Windows 11 24H2",
            DeviceManufacturer: "Microsoft",
            DeviceModel: deviceModel);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = responseFactory;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responseFactory(request));
    }
}
