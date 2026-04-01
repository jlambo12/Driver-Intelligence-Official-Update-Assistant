using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.Recommendations;

public enum RecommendationOutcome
{
    Recommended = 0,
    NotRecommended = 1,
    InsufficientEvidence = 2,
    Incompatible = 3,
    AlreadyUpToDate = 4
}

public enum RecommendationReasonCode
{
    MissingCandidateVersion = 0,
    CandidateNotNewer = 1,
    CandidateHasLowCompatibilityConfidence = 2,
    CandidateIsOfficialSource = 3,
    CandidateIsOemSource = 4,
    CandidateFromLowerPrecedenceSource = 5,
    NoCandidates = 6,
    CompatibleUpgradeAvailable = 7,
    CandidateMarkedIncompatible = 8
}

public sealed record RecommendationReason(
    RecommendationReasonCode Code,
    string Message,
    string? ProviderCode,
    string? CandidateVersion);

public sealed record RecommendationDecision(
    RecommendationOutcome Outcome,
    string InstalledVersion,
    string? RecommendedVersion,
    string? ProviderCode,
    CompatibilityConfidence CompatibilityConfidence,
    SourceEvidence? SourceEvidence,
    IReadOnlyCollection<RecommendationReason> Reasons)
{
    public bool IsRecommendation => Outcome == RecommendationOutcome.Recommended;
}
