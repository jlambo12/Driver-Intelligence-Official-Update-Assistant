using DriverGuardian.Domain.Enums;

namespace DriverGuardian.ProviderAdapters.Abstractions.Models;

public sealed record SourceEvidence(
    DriverSourceProvenance Provenance,
    Uri SourceUri,
    string EvidenceType,
    DateTimeOffset ObservedAtUtc,
    bool IsMachineVerifiable);
