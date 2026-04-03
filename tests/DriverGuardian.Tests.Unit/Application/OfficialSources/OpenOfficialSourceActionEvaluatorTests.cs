using DriverGuardian.Application.OfficialSources;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Tests.Unit.Application.OfficialSources;

public sealed class OpenOfficialSourceActionEvaluatorTests
{
    private readonly OpenOfficialSourceActionEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_ReturnsAllowedAndDirectDriverPage_WhenOfficialPublisherSourceMatchesEvidenceHost()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            sourceUri: new Uri("https://downloads.vendor.test/provider"),
            isOfficial: true,
            trustLevel: SourceTrustLevel.OfficialPublisherSite));

        Assert.Equal(OpenOfficialSourceActionOutcome.Allowed, decision.Outcome);
        Assert.Equal(OfficialSourceResolutionOutcome.ConfirmedDirectOfficialDriverPage, decision.ResolutionOutcome);
        Assert.True(decision.IsAllowed);
        Assert.NotNull(decision.Link);
        Assert.Empty(decision.Blockers);
    }

    [Fact]
    public void Evaluate_ReturnsAllowedAndVendorSupportPage_WhenOemSupportSourceMatchesEvidenceHost()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("https://support.vendor.test/drivers"),
            sourceUri: new Uri("https://support.vendor.test/provider"),
            isOfficial: true,
            trustLevel: SourceTrustLevel.OemSupportPortal));

        Assert.Equal(OpenOfficialSourceActionOutcome.Allowed, decision.Outcome);
        Assert.Equal(OfficialSourceResolutionOutcome.ConfirmedVendorSupportPage, decision.ResolutionOutcome);
        Assert.True(decision.IsAllowed);
        Assert.NotNull(decision.Link);
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
        Assert.Equal(OfficialSourceResolutionOutcome.InsufficientEvidence, decision.ResolutionOutcome);
        Assert.False(decision.IsAllowed);
        Assert.Contains(decision.Blockers, blocker => blocker.Reason == OpenOfficialSourceBlockedReason.SourceTrustUnverified);
    }

    [Fact]
    public void Evaluate_ReturnsAllowed_WhenOperatingSystemCatalogTrustIsUsed()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("https://catalog.windows.update.test/driver"),
            sourceUri: new Uri("https://catalog.windows.update.test/provider"),
            isOfficial: true,
            trustLevel: SourceTrustLevel.OperatingSystemCatalog));

        Assert.Equal(OpenOfficialSourceActionOutcome.Allowed, decision.Outcome);
        Assert.Equal(OfficialSourceResolutionOutcome.ConfirmedVendorSupportPage, decision.ResolutionOutcome);
        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.Blockers);
    }

    [Fact]
    public void Evaluate_ReturnsNonOfficialSource_WhenSourceEvidenceIsNotOfficial()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            sourceUri: new Uri("https://downloads.vendor.test/provider"),
            isOfficial: false));

        Assert.Equal(OpenOfficialSourceActionOutcome.NonOfficialSource, decision.Outcome);
        Assert.Equal(OfficialSourceResolutionOutcome.InsufficientEvidence, decision.ResolutionOutcome);
        Assert.False(decision.IsAllowed);
        Assert.Contains(decision.Blockers, blocker => blocker.Reason == OpenOfficialSourceBlockedReason.SourceMarkedNonOfficial);
    }

    [Fact]
    public void Evaluate_ReturnsMissingUrl_WhenOfficialSourceUriIsMissing()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: null,
            sourceUri: new Uri("https://downloads.vendor.test/provider"),
            isOfficial: true));

        Assert.Equal(OpenOfficialSourceActionOutcome.MissingUrl, decision.Outcome);
        Assert.Equal(OfficialSourceResolutionOutcome.InsufficientEvidence, decision.ResolutionOutcome);
        Assert.False(decision.IsAllowed);
        Assert.Contains(decision.Blockers, blocker => blocker.Reason == OpenOfficialSourceBlockedReason.MissingOfficialSourceUrl);
    }

    [Fact]
    public void Evaluate_ReturnsBlocked_WhenOfficialSourceUrlIsNotHttps()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("http://downloads.vendor.test/catalog/driver"),
            sourceUri: new Uri("https://downloads.vendor.test/provider"),
            isOfficial: true));

        Assert.Equal(OpenOfficialSourceActionOutcome.Blocked, decision.Outcome);
        Assert.Equal(OfficialSourceResolutionOutcome.InsufficientEvidence, decision.ResolutionOutcome);
        Assert.False(decision.IsAllowed);
        Assert.Contains(decision.Blockers, blocker => blocker.Reason == OpenOfficialSourceBlockedReason.UrlIsNotHttps);
    }

    [Fact]
    public void Evaluate_ReturnsBlocked_WhenOfficialSourceUrlHostDiffersFromEvidenceHost()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("https://cdn.vendor.test/catalog/driver"),
            sourceUri: new Uri("https://downloads.vendor.test/provider"),
            isOfficial: true));

        Assert.Equal(OpenOfficialSourceActionOutcome.Blocked, decision.Outcome);
        Assert.Equal(OfficialSourceResolutionOutcome.InsufficientEvidence, decision.ResolutionOutcome);
        Assert.False(decision.IsAllowed);
        Assert.Contains(decision.Blockers, blocker => blocker.Reason == OpenOfficialSourceBlockedReason.UrlHostMismatch);
    }

    [Fact]
    public void Evaluate_ReturnsAllowed_WhenDifferentHostIsExplicitlyAllowed()
    {
        var decision = _evaluator.Evaluate(CreateRequest(
            officialSourceUri: new Uri("https://cdn.vendor.test/catalog/driver"),
            sourceUri: new Uri("https://downloads.vendor.test/provider"),
            isOfficial: true,
            allowDifferentHostOfficialDownload: true));

        Assert.Equal(OpenOfficialSourceActionOutcome.Allowed, decision.Outcome);
        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.Blockers);
    }

    private static OpenOfficialSourceActionRequest CreateRequest(
        Uri? officialSourceUri,
        Uri sourceUri,
        bool isOfficial,
        SourceTrustLevel trustLevel = SourceTrustLevel.OfficialPublisherSite,
        bool allowDifferentHostOfficialDownload = false)
        => new(
            ProviderCode: "official",
            DriverIdentifier: "DRV-1",
            SourceEvidence: new SourceEvidence(
                SourceUri: sourceUri,
                PublisherName: "Vendor",
                TrustLevel: trustLevel,
                IsOfficialSource: isOfficial,
                EvidenceNote: "unit-test"),
            OfficialSourceUri: officialSourceUri,
            AllowDifferentHostOfficialDownload: allowDifferentHostOfficialDownload);
}
