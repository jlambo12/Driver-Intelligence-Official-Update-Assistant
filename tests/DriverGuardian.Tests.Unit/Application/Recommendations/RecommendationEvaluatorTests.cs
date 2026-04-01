using DriverGuardian.Application.Recommendations;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Tests.Unit.Application.Recommendations;

public sealed class RecommendationEvaluatorTests
{
    private readonly RecommendationEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_ShouldReturnInsufficientEvidence_WhenNoCandidates()
    {
        var decision = _evaluator.Evaluate(new RecommendationEvaluationInput(CreateInstalled("1.0.0"), [], ProviderPrecedence.OfficialFirst));

        Assert.Equal(RecommendationOutcome.InsufficientEvidence, decision.Outcome);
        Assert.False(decision.IsRecommendation);
        Assert.Contains(decision.Reasons, reason => reason.Code == RecommendationReasonCode.NoCandidates);
    }

    [Fact]
    public void Evaluate_ShouldReturnRecommended_ForNewerOfficialCompatibleCandidate()
    {
        var decision = _evaluator.Evaluate(new RecommendationEvaluationInput(
            CreateInstalled("1.0.0"),
            [CreateCandidate("official", "2.0.0", CompatibilityConfidence.High, true, SourceTrustLevel.OfficialPublisherSite)],
            ProviderPrecedence.OfficialFirst));

        Assert.Equal(RecommendationOutcome.Recommended, decision.Outcome);
        Assert.True(decision.IsRecommendation);
        Assert.Equal("2.0.0", decision.RecommendedVersion);
        Assert.Contains(decision.Reasons, reason => reason.Code == RecommendationReasonCode.CompatibleUpgradeAvailable);
    }

    [Fact]
    public void Evaluate_ShouldReturnAlreadyUpToDate_WhenTopCandidateIsNotNewer()
    {
        var decision = _evaluator.Evaluate(new RecommendationEvaluationInput(
            CreateInstalled("2.0.0"),
            [CreateCandidate("official", "2.0.0", CompatibilityConfidence.High, true, SourceTrustLevel.OfficialPublisherSite)],
            ProviderPrecedence.OfficialFirst));

        Assert.Equal(RecommendationOutcome.AlreadyUpToDate, decision.Outcome);
        Assert.Null(decision.RecommendedVersion);
    }

    [Fact]
    public void Evaluate_ShouldReturnIncompatible_WhenCandidateConfidenceIsLow()
    {
        var decision = _evaluator.Evaluate(new RecommendationEvaluationInput(
            CreateInstalled("1.0.0"),
            [CreateCandidate("official", "3.0.0", CompatibilityConfidence.Low, true, SourceTrustLevel.OfficialPublisherSite)],
            ProviderPrecedence.OfficialFirst));

        Assert.Equal(RecommendationOutcome.Incompatible, decision.Outcome);
        Assert.Contains(decision.Reasons, reason => reason.Code == RecommendationReasonCode.CandidateMarkedIncompatible);
    }

    [Fact]
    public void Evaluate_ShouldPreferOemCandidate_WhenOemPrecedenceIsConfigured()
    {
        var decision = _evaluator.Evaluate(new RecommendationEvaluationInput(
            CreateInstalled("1.0.0"),
            [
                CreateCandidate("official", "2.0.0", CompatibilityConfidence.High, true, SourceTrustLevel.OfficialPublisherSite),
                CreateCandidate("oem", "2.1.0", CompatibilityConfidence.High, false, SourceTrustLevel.OemSupportPortal)
            ],
            ProviderPrecedence.OemFirst));

        Assert.Equal(RecommendationOutcome.Recommended, decision.Outcome);
        Assert.Equal("oem", decision.ProviderCode);
        Assert.Equal("2.1.0", decision.RecommendedVersion);
    }

    private static InstalledDriverSnapshot CreateInstalled(string version)
        => new(
            new DeviceIdentity("TEST\\DEV\\1"),
            new HardwareIdentifier("PCI\\VEN_1234&DEV_1111"),
            version,
            null,
            "Vendor");

    private static RecommendationCandidateInput CreateCandidate(
        string providerCode,
        string version,
        CompatibilityConfidence confidence,
        bool isOfficialSource,
        SourceTrustLevel trustLevel)
        => new(
            providerCode,
            new ProviderCandidate(
                DriverIdentifier: "DRV-1",
                CandidateVersion: version,
                ReleaseDateIso: null,
                CompatibilityConfidence: confidence,
                SourceEvidence: new SourceEvidence(
                    new Uri("https://example.test/driver"),
                    "Test Publisher",
                    trustLevel,
                    isOfficialSource,
                    "unit-test")));
}
