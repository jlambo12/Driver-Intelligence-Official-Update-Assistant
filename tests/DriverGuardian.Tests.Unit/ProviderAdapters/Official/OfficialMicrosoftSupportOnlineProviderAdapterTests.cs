using System.Net;
using System.Net.Http;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Official.Registry;

namespace DriverGuardian.Tests.Unit.ProviderAdapters.Official;

public sealed class OfficialMicrosoftSupportOnlineProviderAdapterTests
{
    [Fact]
    public async Task LookupAsync_ReturnsOfficialCandidate_WhenEndpointRespondsSuccessfully()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var adapter = new OfficialMicrosoftSupportOnlineProviderAdapter(new HttpClient(handler));

        var response = await adapter.LookupAsync(CreateRequest("PCI\\VEN_10EC&DEV_8168", "RTL8168"), CancellationToken.None);

        Assert.True(response.IsSuccess);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal(SourceTrustLevel.OfficialPublisherSite, candidate.SourceEvidence.TrustLevel);
        Assert.Equal("support.microsoft.com", candidate.SourceEvidence.SourceUri.Host);
        Assert.Equal(HardwareIdMatchStrength.NormalizedHardwareId, candidate.MatchStrength);
        Assert.Equal(CompatibilityConfidence.Medium, candidate.CompatibilityConfidence);
        Assert.Contains("PCI%5CVEN_10EC%26DEV_8168", candidate.SourceEvidence.SourceUri.Query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_RetriesTransientFailure_AndReturnsCandidateOnRecovery()
    {
        var callCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            callCount++;
            return callCount == 1
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });

        var adapter = new OfficialMicrosoftSupportOnlineProviderAdapter(new HttpClient(handler));

        var response = await adapter.LookupAsync(CreateRequest("PCI\\VEN_8086&DEV_A2AF", "Intel Model"), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(2, callCount);
        Assert.Single(response.Candidates);
    }

    [Fact]
    public async Task LookupAsync_ReturnsEmpty_WhenNoHintsProvided()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Network should not be used."));
        var adapter = new OfficialMicrosoftSupportOnlineProviderAdapter(new HttpClient(handler));

        var response = await adapter.LookupAsync(CreateRequest(null, null), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Empty(response.Candidates);
    }

    [Fact]
    public async Task LookupAsync_ManufacturerOnlyHint_ReturnsUnknownConfidenceManufacturerMatch()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var adapter = new OfficialMicrosoftSupportOnlineProviderAdapter(new HttpClient(handler));

        var response = await adapter.LookupAsync(CreateRequest(null, null, manufacturer: "Contoso"), CancellationToken.None);

        Assert.True(response.IsSuccess);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal(HardwareIdMatchStrength.ManufacturerPortalHint, candidate.MatchStrength);
        Assert.Equal(CompatibilityConfidence.Unknown, candidate.CompatibilityConfidence);
        Assert.Contains("manufacturer", candidate.SourceEvidence.EvidenceNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_OpensCircuitBreaker_AfterRepeatedTransientFailures()
    {
        var callCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });

        var adapter = new OfficialMicrosoftSupportOnlineProviderAdapter(new HttpClient(handler));

        var first = await adapter.LookupAsync(CreateRequest("PCI\\VEN_8086&DEV_A2AF", "Intel Model"), CancellationToken.None);
        Assert.False(first.IsSuccess);
        Assert.Equal(3, callCount);

        var second = await adapter.LookupAsync(CreateRequest("PCI\\VEN_10EC&DEV_8168", "RTL8168"), CancellationToken.None);
        Assert.False(second.IsSuccess);
        Assert.Contains("circuit is open", second.FailureReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, callCount);
    }

    private static ProviderLookupRequest CreateRequest(string? hardwareId, string? model, string manufacturer = "Microsoft")
    {
        var hardwareIds = string.IsNullOrWhiteSpace(hardwareId)
            ? Array.Empty<string>()
            : [hardwareId];

        return new ProviderLookupRequest(
            ProviderCode: "microsoft-support-online",
            DeviceInstanceId: "device-1",
            HardwareIds: hardwareIds,
            InstalledDriverVersion: "1.0.0",
            OperatingSystemVersion: "Windows 11 24H2",
            DeviceManufacturer: manufacturer,
            DeviceModel: model);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = responseFactory;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responseFactory(request));
    }
}
