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
            Content = new StringContent("<html><head><title>Microsoft Update Catalog</title></head><body></body></html>", Encoding.UTF8, "text/html")
        });

        var adapter = new OfficialWindowsCatalogOnlineProviderAdapter(new HttpClient(handler));

        var response = await adapter.LookupAsync(CreateRequest("PCI\\VEN_8086&DEV_A2AF"), CancellationToken.None);

        Assert.True(response.IsSuccess);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal(SourceTrustLevel.OperatingSystemCatalog, candidate.SourceEvidence.TrustLevel);
        Assert.Contains("catalog.update.microsoft.com", candidate.SourceEvidence.SourceUri.Host, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PCI%5CVEN_8086%26DEV_A2AF", candidate.SourceEvidence.SourceUri.Query, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Microsoft Update Catalog", candidate.SourceEvidence.EvidenceNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_ReturnsFailure_WhenCatalogReturnsServerError()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("maintenance", Encoding.UTF8, "text/plain")
        });

        var adapter = new OfficialWindowsCatalogOnlineProviderAdapter(new HttpClient(handler));

        var response = await adapter.LookupAsync(CreateRequest("PCI\\VEN_10EC&DEV_8168"), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Empty(response.Candidates);
        Assert.Contains("503", response.FailureReason);
    }

    [Fact]
    public async Task LookupAsync_ReturnsEmpty_WhenNoSearchHintProvided()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not hit network without query."));
        var adapter = new OfficialWindowsCatalogOnlineProviderAdapter(new HttpClient(handler));

        var response = await adapter.LookupAsync(CreateRequest(null), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Empty(response.Candidates);
    }

    private static ProviderLookupRequest CreateRequest(string? hardwareId)
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
            DeviceModel: null);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = responseFactory;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responseFactory(request));
    }
}
