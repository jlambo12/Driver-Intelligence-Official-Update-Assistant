using System.Net;
using System.Net.Http;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Official.Registry;

namespace DriverGuardian.Tests.Unit.ProviderAdapters.Official;

public sealed class OfficialWindowsCatalogProviderAdapterTests
{
    [Fact]
    public async Task LookupAsync_ReturnsCandidate_ForSearchPageWithVersionEvidence()
    {
        using var httpClient = CreateHttpClient("<html><body><div>Version: 31.0.101.2125</div></body></html>");
        var adapter = new OfficialWindowsCatalogProviderAdapter(httpClient);

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-1",
                HardwareIds: ["PCI\\VEN_8086&DEV_15F3"],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Intel",
                DeviceModel: null),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal("31.0.101.2125", candidate.CandidateVersion);
        Assert.Equal(SourceTrustLevel.OperatingSystemCatalog, candidate.SourceEvidence.TrustLevel);
        Assert.True(candidate.SourceEvidence.IsOfficialSource);
        Assert.Equal("catalog.update.microsoft.com", candidate.SourceEvidence.SourceUri.Host);
    }

    [Fact]
    public async Task LookupAsync_ReturnsEmpty_WhenSearchPageHasNoParseableVersion()
    {
        using var httpClient = CreateHttpClient("<html><body>No results found.</body></html>");
        var adapter = new OfficialWindowsCatalogProviderAdapter(httpClient);

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-2",
                HardwareIds: ["PCI\\VEN_9999&DEV_0001"],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Unknown",
                DeviceModel: null),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Empty(response.Candidates);
        Assert.Null(response.FailureReason);
    }

    [Fact]
    public async Task LookupAsync_ReturnsExplicitFailure_WhenHardwareIdsMissing()
    {
        using var httpClient = CreateHttpClient("<html></html>");
        var adapter = new OfficialWindowsCatalogProviderAdapter(httpClient);

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-3",
                HardwareIds: [],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Unknown",
                DeviceModel: null),
            CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Empty(response.Candidates);
        Assert.Contains("hardware id", response.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_ReturnsFailure_WhenCatalogRequestFails()
    {
        using var httpClient = CreateHttpClient(
            body: "",
            statusCode: HttpStatusCode.BadGateway);
        var adapter = new OfficialWindowsCatalogProviderAdapter(httpClient);

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-4",
                HardwareIds: ["PCI\\VEN_8086&DEV_15F3"],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Intel",
                DeviceModel: null),
            CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Empty(response.Candidates);
        Assert.Contains("request failed", response.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClient CreateHttpClient(string body, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(new StubHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body)
            }));

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
