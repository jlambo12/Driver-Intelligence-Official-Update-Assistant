using DriverGuardian.Domain.Enums;
using DriverGuardian.Domain.ValueObjects;

namespace DriverGuardian.Domain.Entities;

public sealed record InstalledDriverSnapshot(
    DeviceIdentity Device,
    IReadOnlyCollection<HardwareIdentifier> HardwareIds,
    string DriverVersion,
    DateOnly? DriverDate,
    string ProviderName,
    DriverSourceProvenance SourceProvenance,
    CompatibilityConfidence CompatibilityConfidence,
    bool IsSigned,
    string? SignatureIssuer);
