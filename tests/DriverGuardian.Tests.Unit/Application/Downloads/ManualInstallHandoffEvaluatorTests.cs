using DriverGuardian.Application.Downloads;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Tests.Unit.Application.Downloads;

public sealed class ManualInstallHandoffEvaluatorTests
{
    private readonly ManualInstallHandoffEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_ReturnsReadyForManualInstallHandoff_WhenCandidateHasOfficialHttpsPackageReferenceFromSameHost()
    {
        var decision = _evaluator.Evaluate(new ManualInstallHandoffRequest("official", CreateCandidate(
            downloadUri: new Uri("https://downloads.vendor.test/driver-2.0.0.cab"),
            sourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            isOfficial: true)));

        Assert.Equal(HandoffReadinessOutcome.ReadyForManualInstallHandoff, decision.Outcome);
        Assert.True(decision.IsHandoffReady);
        Assert.NotNull(decision.PackageReference);
        Assert.Empty(decision.Reasons);
    }


    [Fact]
    public void Evaluate_ReturnsInsufficientEvidence_WhenSourceTrustIsUnknown()
    {
        var decision = _evaluator.Evaluate(new ManualInstallHandoffRequest("official", CreateCandidate(
            downloadUri: new Uri("https://downloads.vendor.test/driver-2.0.0.cab"),
            sourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            isOfficial: true,
            trustLevel: SourceTrustLevel.Unknown)));

        Assert.Equal(HandoffReadinessOutcome.InsufficientEvidence, decision.Outcome);
        Assert.False(decision.IsHandoffReady);
        Assert.Contains(decision.Reasons, reason => reason.Reason == HandoffBlockReason.SourceTrustUnverified);
    }

    [Fact]
    public void Evaluate_ReturnsNonOfficialSource_WhenSourceEvidenceIsNotOfficial()
    {
        var decision = _evaluator.Evaluate(new ManualInstallHandoffRequest("official", CreateCandidate(
            downloadUri: new Uri("https://downloads.vendor.test/driver-2.0.0.cab"),
            sourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            isOfficial: false)));

        Assert.Equal(HandoffReadinessOutcome.NonOfficialSource, decision.Outcome);
        Assert.False(decision.IsHandoffReady);
        Assert.Contains(decision.Reasons, reason => reason.Reason == HandoffBlockReason.SourceMarkedNonOfficial);
    }

    [Fact]
    public void Evaluate_ReturnsMissingOfficialPackageReference_WhenDownloadUrlIsMissing()
    {
        var decision = _evaluator.Evaluate(new ManualInstallHandoffRequest("official", CreateCandidate(
            downloadUri: null,
            sourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            isOfficial: true)));

        Assert.Equal(HandoffReadinessOutcome.MissingOfficialPackageReference, decision.Outcome);
        Assert.False(decision.IsHandoffReady);
        Assert.Contains(decision.Reasons, reason => reason.Reason == HandoffBlockReason.MissingOfficialPackageUrl);
    }

    [Fact]
    public void Evaluate_ReturnsUserActionRequired_WhenPackageUrlIsNotHttps()
    {
        var decision = _evaluator.Evaluate(new ManualInstallHandoffRequest("official", CreateCandidate(
            downloadUri: new Uri("http://downloads.vendor.test/driver-2.0.0.cab"),
            sourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            isOfficial: true)));

        Assert.Equal(HandoffReadinessOutcome.UserActionRequired, decision.Outcome);
        Assert.False(decision.IsHandoffReady);
        Assert.Contains(decision.Reasons, reason => reason.Reason == HandoffBlockReason.PackageUrlIsNotHttps);
    }

    [Fact]
    public void Evaluate_ReturnsUserActionRequired_WhenPackageUrlHostDiffersFromSourceEvidenceHost()
    {
        var decision = _evaluator.Evaluate(new ManualInstallHandoffRequest("official", CreateCandidate(
            downloadUri: new Uri("https://cdn.vendor.test/driver-2.0.0.cab"),
            sourceUri: new Uri("https://downloads.vendor.test/catalog/driver"),
            isOfficial: true)));

        Assert.Equal(HandoffReadinessOutcome.UserActionRequired, decision.Outcome);
        Assert.False(decision.IsHandoffReady);
        Assert.Contains(decision.Reasons, reason => reason.Reason == HandoffBlockReason.PackageUrlHostMismatch);
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
            HardwareMatchQuality: HardwareMatchQuality.ExactHardwareId,
            SourceEvidence: new SourceEvidence(
                sourceUri,
                "Vendor",
                trustLevel,
                isOfficial,
                "unit-test"),
            DownloadUri: downloadUri);
}
