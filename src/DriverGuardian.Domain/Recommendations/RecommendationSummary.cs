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
        Uri? evidenceSourceUri = null,
        string? evidencePublisherName = null,
        int? evidenceTrustLevel = null,
        bool? evidenceIsOfficialSource = null,
        string? evidenceNote = null,
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
        EvidenceSourceUri = evidenceSourceUri;
        EvidencePublisherName = evidencePublisherName;
        EvidenceTrustLevel = evidenceTrustLevel;
        EvidenceIsOfficialSource = evidenceIsOfficialSource;
        EvidenceNote = evidenceNote;
        OfficialSourceUri = officialSourceUri;
    }

    public DeviceIdentity DeviceIdentity { get; }
    public bool HasRecommendation { get; }
    public string Reason { get; }
    public string? RecommendedVersion { get; }
    public string? ProviderCode { get; }
    public Uri? EvidenceSourceUri { get; }
    public string? EvidencePublisherName { get; }
    public int? EvidenceTrustLevel { get; }
    public bool? EvidenceIsOfficialSource { get; }
    public string? EvidenceNote { get; }
    public Uri? OfficialSourceUri { get; }
}
