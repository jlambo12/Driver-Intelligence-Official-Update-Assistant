namespace DriverGuardian.ProviderAdapters.Abstractions.Lookup;

public enum CompatibilityConfidence
{
    Unknown = 0,
    Low = 1,
    Medium = 2,
    High = 3
}

public enum HardwareIdMatchStrength
{
    Unknown = 0,
    VendorFallback = 1,
    NormalizedHardwareId = 2,
    ExactHardwareId = 3,
    ManufacturerPortalHint = 4
}

public sealed record ProviderCandidate(
    string DriverIdentifier,
    string? CandidateVersion,
    string? ReleaseDateIso,
    CompatibilityConfidence CompatibilityConfidence,
    HardwareIdMatchStrength MatchStrength,
    string ConfidenceRationale,
    SourceEvidence SourceEvidence,
    Uri? DownloadUri);

public sealed record ProviderLookupResponse(
    string ProviderCode,
    bool IsSuccess,
    IReadOnlyCollection<ProviderCandidate> Candidates,
    string? FailureReason);
