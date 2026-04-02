namespace DriverGuardian.Domain.Recommendations;

public enum RecommendationSourceTrustLevel
{
    Unknown = 0,
    OfficialPublisherSite = 1,
    OemSupportPortal = 2,
    OperatingSystemCatalog = 3
}

public sealed record RecommendationSourceEvidence(
    string ProviderCode,
    Uri SourceUri,
    string PublisherName,
    RecommendationSourceTrustLevel TrustLevel,
    bool IsOfficialSource,
    string EvidenceNote,
    Uri? OfficialSourceUri);
