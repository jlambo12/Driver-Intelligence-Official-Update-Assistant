using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.OfficialSources;

public sealed record OpenOfficialSourceActionRequest(
    string ProviderCode,
    string DriverIdentifier,
    SourceEvidence SourceEvidence,
    Uri? OfficialSourceUri,
    OfficialSourceResolutionOutcome ResolutionOutcome);
