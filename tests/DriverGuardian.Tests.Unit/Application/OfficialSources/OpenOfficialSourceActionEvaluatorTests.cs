using DriverGuardian.Application.OfficialSources;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Tests.Unit.Application.OfficialSources;

public sealed class OpenOfficialSourceActionEvaluatorTests
{
    private readonly OpenOfficialSourceActionEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_ReturnsAllowed_WithDirectOfficialDriverPageOutcome()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            sourceUri: new Uri("https://downloads.vendor.test/provider"),
            resolutionOutcome: OfficialSourceResolutionOutcome.DirectOfficialDriverPageConfirmed));

        Assert.Equal(OpenOfficialSourceActionOutcome.Allowed, decision.Outcome);
        Assert.Equal(OfficialSourceResolutionOutcome.DirectOfficialDriverPageConfirmed, decision.ResolutionOutcome);
        Assert.True(decision.IsAllowed);
        Assert.NotNull(decision.Link);
    }

    [Fact]
    public void Evaluate_ReturnsAllowed_WithVendorSupportPageOutcome()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("https://downloads.vendor.test/support"),
            sourceUri: new Uri("https://downloads.vendor.test/provider"),
            trustLevel: SourceTrustLevel.OemSupportPortal,
            resolutionOutcome: OfficialSourceResolutionOutcome.VendorSupportPageConfirmed));

        Assert.Equal(OpenOfficialSourceActionOutcome.Allowed, decision.Outcome);
        Assert.Equal(OfficialSourceResolutionOutcome.VendorSupportPageConfirmed, decision.ResolutionOutcome);
        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public void Evaluate_ReturnsInsufficientEvidence_WhenResolutionOutcomeIsInsufficient()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            sourceUri: new Uri("https://downloads.vendor.test/provider"),
            resolutionOutcome: OfficialSourceResolutionOutcome.InsufficientEvidence));

        Assert.Equal(OpenOfficialSourceActionOutcome.InsufficientEvidence, decision.Outcome);
        Assert.False(decision.IsAllowed);
        Assert.Contains(decision.Blockers, blocker => blocker.Reason == OpenOfficialSourceBlockedReason.ResolutionNotConfirmed);
    }

    [Fact]
    public void Evaluate_ReturnsMissingUrl_WhenOfficialSourceUriIsMissing()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: null,
            sourceUri: new Uri("https://downloads.vendor.test/provider"),
            resolutionOutcome: OfficialSourceResolutionOutcome.DirectOfficialDriverPageConfirmed));

        Assert.Equal(OpenOfficialSourceActionOutcome.MissingUrl, decision.Outcome);
        Assert.False(decision.IsAllowed);
    }

    private static OpenOfficialSourceActionRequest CreateRequest(
        Uri? officialSourceUri,
        Uri sourceUri,
        OfficialSourceResolutionOutcome resolutionOutcome,
        SourceTrustLevel trustLevel = SourceTrustLevel.OfficialPublisherSite)
        => new(
            ProviderCode: "official",
            DriverIdentifier: "DRV-1",
            SourceEvidence: new SourceEvidence(
                SourceUri: sourceUri,
                PublisherName: "Vendor",
                TrustLevel: trustLevel,
                IsOfficialSource: true,
                EvidenceNote: "unit-test"),
            OfficialSourceUri: officialSourceUri,
            ResolutionOutcome: resolutionOutcome);
}
