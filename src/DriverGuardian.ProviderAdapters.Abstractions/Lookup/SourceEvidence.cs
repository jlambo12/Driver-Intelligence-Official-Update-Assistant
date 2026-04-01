namespace DriverGuardian.ProviderAdapters.Abstractions.Lookup;

public enum SourceTrustLevel
{
    Unknown = 0,
    OfficialPublisherSite = 1,
    OemSupportPortal = 2,
    OperatingSystemCatalog = 3
}

public sealed record SourceEvidence(
    Uri SourceUri,
    string PublisherName,
    SourceTrustLevel TrustLevel,
    bool IsOfficialSource,
    string EvidenceNote);
