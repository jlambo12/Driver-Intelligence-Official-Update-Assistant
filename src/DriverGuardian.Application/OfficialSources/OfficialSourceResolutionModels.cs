using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.OfficialSources;

public sealed record OfficialSourceResolutionRequest(
    string DeviceInstanceId,
    string HardwareId,
    string? InstalledDriverVersion,
    string? DeviceManufacturer);

public sealed record OfficialSourceResolutionResult(
    OpenOfficialSourceActionDecision Decision,
    SourceEvidence? SourceEvidence,
    Uri? OfficialSourceUri);
