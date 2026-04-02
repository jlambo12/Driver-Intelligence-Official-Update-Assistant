using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.Domain.Devices;

namespace DriverGuardian.Domain.Recommendations;

public sealed record RecommendationSummary
{
    public RecommendationSummary(
        DeviceIdentity deviceIdentity,
        bool hasRecommendation,
        string reason,
        string? recommendedVersion,
        string? providerCode = null,
        SourceEvidence? sourceEvidence = null,
        Uri? officialSourceUri = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        DeviceIdentity = deviceIdentity;
        HasRecommendation = hasRecommendation;
        Reason = reason.Trim();
        RecommendedVersion = recommendedVersion;
        ProviderCode = providerCode;
        SourceEvidence = sourceEvidence;
        OfficialSourceUri = officialSourceUri;
    }

    public DeviceIdentity DeviceIdentity { get; }
    public bool HasRecommendation { get; }
    public string Reason { get; }
    public string? RecommendedVersion { get; }
    public string? ProviderCode { get; }
    public SourceEvidence? SourceEvidence { get; }
    public Uri? OfficialSourceUri { get; }
}
