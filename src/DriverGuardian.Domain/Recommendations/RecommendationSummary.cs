using DriverGuardian.Domain.Devices;

namespace DriverGuardian.Domain.Recommendations;

public enum RecommendationSummaryReasonCode
{
    Unknown = 0,
    RecommendedUpgradeAvailable = 1,
    AlreadyUpToDate = 2,
    CandidateMarkedIncompatible = 3,
    CandidateCompatibilityUnknown = 4,
    InsufficientEvidence = 5,
    InsufficientEvidenceDueToProviderFailures = 6
}

public sealed record RecommendationSummary
{
    public RecommendationSummary(
        DeviceIdentity deviceIdentity,
        bool hasRecommendation,
        string reason,
        string? recommendedVersion,
        string? officialSourceUrl = null,
        RecommendationSummaryReasonCode reasonCode = RecommendationSummaryReasonCode.Unknown)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        DeviceIdentity = deviceIdentity;
        HasRecommendation = hasRecommendation;
        Reason = reason.Trim();
        RecommendedVersion = recommendedVersion;
        OfficialSourceUrl = string.IsNullOrWhiteSpace(officialSourceUrl) ? null : officialSourceUrl.Trim();
        ReasonCode = reasonCode == RecommendationSummaryReasonCode.Unknown && hasRecommendation
            ? RecommendationSummaryReasonCode.RecommendedUpgradeAvailable
            : reasonCode;
    }

    public DeviceIdentity DeviceIdentity { get; }
    public bool HasRecommendation { get; }
    public string Reason { get; }
    public string? RecommendedVersion { get; }
    public string? OfficialSourceUrl { get; }
    public RecommendationSummaryReasonCode ReasonCode { get; }
}
