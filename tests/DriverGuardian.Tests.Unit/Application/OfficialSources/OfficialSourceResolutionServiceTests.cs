using DriverGuardian.Application.OfficialSources;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.Tests.Unit.Application.OfficialSources;

public sealed class OfficialSourceResolutionServiceTests
{
    [Fact]
    public async Task ResolveAsync_WhenProviderReturnsOfficialPublisherCandidate_ShouldReturnConfirmedDirectPage()
    {
        var service = new OfficialSourceResolutionService(
            [new StubProviderAdapter(CreateCandidate(SourceTrustLevel.OfficialPublisherSite, new Uri("https://downloads.vendor.example/driver")))],
            new OpenOfficialSourceActionEvaluator());

        var result = await service.ResolveAsync(
            new OfficialSourceResolutionRequest(
                "PCI\\VEN_1234&DEV_ABCD",
                "PCI\\VEN_1234&DEV_ABCD",
                "1.0.0",
                "Vendor"),
            CancellationToken.None);

        Assert.True(result.Decision.IsAllowed);
        Assert.Equal(OfficialSourceResolutionOutcome.ConfirmedDirectOfficialDriverPage, result.Decision.ResolutionOutcome);
        Assert.Equal("https://downloads.vendor.example/driver", result.Decision.Link?.OfficialSourceUri.ToString());
        Assert.NotNull(result.SourceEvidence);
    }

    [Fact]
    public async Task ResolveAsync_WhenOnlyOperatingSystemCatalogEvidenceExists_ShouldReturnInsufficientEvidence()
    {
        var service = new OfficialSourceResolutionService(
            [new StubProviderAdapter(CreateCandidate(SourceTrustLevel.OperatingSystemCatalog, new Uri("https://catalog.update.microsoft.com/Search.aspx?q=abc")))],
            new OpenOfficialSourceActionEvaluator());

        var result = await service.ResolveAsync(
            new OfficialSourceResolutionRequest(
                "PCI\\VEN_1234&DEV_ABCD",
                "PCI\\VEN_1234&DEV_ABCD",
                "1.0.0",
                "Vendor"),
            CancellationToken.None);

        Assert.False(result.Decision.IsAllowed);
        Assert.Equal(OfficialSourceResolutionOutcome.InsufficientEvidence, result.Decision.ResolutionOutcome);
        Assert.Equal(SourceTrustLevel.OperatingSystemCatalog, result.SourceEvidence?.TrustLevel);
    }

    private static ProviderCandidate CreateCandidate(SourceTrustLevel trustLevel, Uri sourceUri)
        => new(
            "driver-id",
            "2.0.0",
            null,
            CompatibilityConfidence.Medium,
            new SourceEvidence(sourceUri, "Vendor", trustLevel, true, "Evidence note"),
            null);

    private sealed class StubProviderAdapter(ProviderCandidate candidate) : IOfficialProviderAdapter
    {
        public ProviderDescriptor Descriptor => new(
            "stub-provider",
            "Stub Provider",
            true,
            true,
            ProviderPrecedence.HardwareVendor);

        public Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ProviderLookupResponse(Descriptor.Code, true, [candidate], null));
    }
}
