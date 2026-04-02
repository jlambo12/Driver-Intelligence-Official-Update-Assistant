using DriverGuardian.Application.OfficialSources;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Tests.Unit.Application.OfficialSources;

public sealed class OpenOfficialSourceActionEvaluatorTests
{
    private readonly OpenOfficialSourceActionEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_ReturnsDirectOfficialDriverPageConfirmed_WhenTrustLevelIsOfficialPublisherSite()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            sourceUri: new Uri("https://downloads.vendor.test/provider"),
            isOfficial: true,
            trustLevel: SourceTrustLevel.OfficialPublisherSite));

        Assert.Equal(OpenOfficialSourceActionOutcome.Allowed, decision.Outcome);
        Assert.Equal(OfficialSourceResolutionKind.DirectOfficialDriverPageConfirmed, decision.Resolution);
        Assert.True(decision.IsAllowed);
        Assert.True(decision.IsReadyForOpen);
        Assert.NotNull(decision.Link);
        Assert.Empty(decision.Blockers);
    }

    [Fact]
    public void Evaluate_ReturnsVendorSupportPageConfirmed_WhenTrustLevelIsOemSupportPortal()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("https://support.vendor.test/drivers/device"),
            sourceUri: new Uri("https://support.vendor.test/driver-support"),
            isOfficial: true,
            trustLevel: SourceTrustLevel.OemSupportPortal));

        Assert.Equal(OpenOfficialSourceActionOutcome.Allowed, decision.Outcome);
        Assert.Equal(OfficialSourceResolutionKind.VendorSupportPageConfirmed, decision.Resolution);
        Assert.True(decision.IsReadyForOpen);
        Assert.Empty(decision.Blockers);
    }

    [Fact]
    public void Evaluate_ReturnsInsufficientEvidence_WhenTrustLevelIsUnknown()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            sourceUri: new Uri("https://downloads.vendor.test/provider"),
            isOfficial: true,
            trustLevel: SourceTrustLevel.Unknown));

        Assert.Equal(OpenOfficialSourceActionOutcome.InsufficientEvidence, decision.Outcome);
        Assert.Equal(OfficialSourceResolutionKind.InsufficientEvidence, decision.Resolution);
        Assert.False(decision.IsAllowed);
        Assert.False(decision.IsReadyForOpen);
        Assert.Contains(decision.Blockers, blocker => blocker.Reason == OpenOfficialSourceBlockedReason.SourceTrustUnverified);
    }

    [Fact]
    public void Evaluate_ReturnsInsufficientEvidence_WhenTrustLevelDoesNotRepresentVendorDriverPage()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("https://catalog.update.microsoft.com/Search.aspx?q=device"),
            sourceUri: new Uri("https://catalog.update.microsoft.com/"),
            isOfficial: true,
            trustLevel: SourceTrustLevel.OperatingSystemCatalog));

        Assert.Equal(OpenOfficialSourceActionOutcome.InsufficientEvidence, decision.Outcome);
        Assert.Equal(OfficialSourceResolutionKind.InsufficientEvidence, decision.Resolution);
        Assert.False(decision.IsReadyForOpen);
        Assert.Contains(decision.Blockers, blocker => blocker.Reason == OpenOfficialSourceBlockedReason.TrustLevelNotSupportedForOfficialSourceAction);
    }

    private static OpenOfficialSourceActionRequest CreateRequest(
        Uri? officialSourceUri,
        Uri sourceUri,
        bool isOfficial,
        SourceTrustLevel trustLevel = SourceTrustLevel.OfficialPublisherSite)
        => new(
            ProviderCode: "official",
            DriverIdentifier: "DRV-1",
            SourceEvidence: new SourceEvidence(
                SourceUri: sourceUri,
                PublisherName: "Vendor",
                TrustLevel: trustLevel,
                IsOfficialSource: isOfficial,
                EvidenceNote: "unit-test"),
            OfficialSourceUri: officialSourceUri);
}
