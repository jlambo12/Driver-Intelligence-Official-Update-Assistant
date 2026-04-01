namespace DriverGuardian.ProviderAdapters.Abstractions.Models;

public sealed record ProviderLookupResponse(
    string? RecommendedVersion,
    CompatibilityAssessmentResult Compatibility,
    IReadOnlyCollection<SourceEvidence> Evidence,
    bool IsAmbiguous,
    string SummaryCode);
