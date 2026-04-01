using DriverGuardian.Application.Downloads;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Tests.Unit.Application.Downloads;

public sealed class SafeDownloadPreparationEvaluatorTests
{
    private readonly SafeDownloadPreparationEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_ReturnsAllowed_WhenCandidateHasOfficialHttpsDownloadFromSameHost()
    {
        var decision = _evaluator.Evaluate(new DownloadPreparationRequest("official", CreateCandidate(
            downloadUri: new Uri("https://downloads.vendor.test/driver-2.0.0.cab"),
            sourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            isOfficial: true)));

        Assert.Equal(DownloadDecisionOutcome.Allowed, decision.Outcome);
        Assert.True(decision.CanPrepareDownload);
        Assert.NotNull(decision.Candidate);
        Assert.Empty(decision.Reasons);
    }


    [Fact]
    public void Evaluate_ReturnsInsufficientEvidence_WhenSourceTrustIsUnknown()
    {
        var decision = _evaluator.Evaluate(new DownloadPreparationRequest("official", CreateCandidate(
            downloadUri: new Uri("https://downloads.vendor.test/driver-2.0.0.cab"),
            sourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            isOfficial: true,
            trustLevel: SourceTrustLevel.Unknown)));

        Assert.Equal(DownloadDecisionOutcome.InsufficientEvidence, decision.Outcome);
        Assert.False(decision.CanPrepareDownload);
        Assert.Contains(decision.Reasons, reason => reason.Reason == BlockedDownloadReason.SourceTrustUnverified);
    }

    [Fact]
    public void Evaluate_ReturnsNonOfficialSource_WhenSourceEvidenceIsNotOfficial()
    {
        var decision = _evaluator.Evaluate(new DownloadPreparationRequest("official", CreateCandidate(
            downloadUri: new Uri("https://downloads.vendor.test/driver-2.0.0.cab"),
            sourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            isOfficial: false)));

        Assert.Equal(DownloadDecisionOutcome.NonOfficialSource, decision.Outcome);
        Assert.False(decision.CanPrepareDownload);
        Assert.Contains(decision.Reasons, reason => reason.Reason == BlockedDownloadReason.SourceMarkedNonOfficial);
    }

    [Fact]
    public void Evaluate_ReturnsMissingUrl_WhenDownloadUrlIsMissing()
    {
        var decision = _evaluator.Evaluate(new DownloadPreparationRequest("official", CreateCandidate(
            downloadUri: null,
            sourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            isOfficial: true)));

        Assert.Equal(DownloadDecisionOutcome.MissingUrl, decision.Outcome);
        Assert.False(decision.CanPrepareDownload);
        Assert.Contains(decision.Reasons, reason => reason.Reason == BlockedDownloadReason.MissingDownloadUrl);
    }

    [Fact]
    public void Evaluate_ReturnsBlocked_WhenDownloadUrlIsNotHttps()
    {
        var decision = _evaluator.Evaluate(new DownloadPreparationRequest("official", CreateCandidate(
            downloadUri: new Uri("http://downloads.vendor.test/driver-2.0.0.cab"),
            sourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            isOfficial: true)));

        Assert.Equal(DownloadDecisionOutcome.Blocked, decision.Outcome);
        Assert.False(decision.CanPrepareDownload);
        Assert.Contains(decision.Reasons, reason => reason.Reason == BlockedDownloadReason.DownloadUrlIsNotHttps);
    }

    [Fact]
    public void Evaluate_ReturnsBlocked_WhenDownloadHostDiffersFromSourceEvidenceHost()
    {
        var decision = _evaluator.Evaluate(new DownloadPreparationRequest("official", CreateCandidate(
            downloadUri: new Uri("https://cdn.vendor.test/driver-2.0.0.cab"),
            sourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            isOfficial: true)));

        Assert.Equal(DownloadDecisionOutcome.Blocked, decision.Outcome);
        Assert.False(decision.CanPrepareDownload);
        Assert.Contains(decision.Reasons, reason => reason.Reason == BlockedDownloadReason.DownloadUrlHostMismatch);
    }

    private static ProviderCandidate CreateCandidate(
        Uri? downloadUri,
        Uri sourceUri,
        bool isOfficial,
        SourceTrustLevel trustLevel = SourceTrustLevel.OfficialPublisherSite)
        => new(
            DriverIdentifier: "DRV-1",
            CandidateVersion: "2.0.0",
            ReleaseDateIso: null,
            CompatibilityConfidence: CompatibilityConfidence.High,
            SourceEvidence: new SourceEvidence(
                sourceUri,
                "Vendor",
                trustLevel,
                isOfficial,
                "unit-test"),
            DownloadUri: downloadUri);
}
