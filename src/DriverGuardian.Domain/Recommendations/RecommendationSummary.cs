using DriverGuardian.Domain.Devices;

namespace DriverGuardian.Domain.Recommendations;

public sealed record RecommendationSummary
{
    public RecommendationSummary(
        DeviceIdentity deviceIdentity,
        bool hasRecommendation,
        string reason,
        string? recommendedVersion,
        RecommendationSourceEvidence? sourceEvidence = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        DeviceIdentity = deviceIdentity;
        HasRecommendation = hasRecommendation;
        Reason = reason.Trim();
        RecommendedVersion = recommendedVersion;
        SourceEvidence = sourceEvidence;
    }

    public DeviceIdentity DeviceIdentity { get; }
    public bool HasRecommendation { get; }
    public string Reason { get; }
    public string? RecommendedVersion { get; }
    public RecommendationSourceEvidence? SourceEvidence { get; }
}
