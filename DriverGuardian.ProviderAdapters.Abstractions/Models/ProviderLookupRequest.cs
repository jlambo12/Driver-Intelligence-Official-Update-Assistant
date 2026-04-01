using DriverGuardian.Domain.ValueObjects;

namespace DriverGuardian.ProviderAdapters.Abstractions.Models;

public sealed record ProviderLookupRequest(
    DeviceIdentity Device,
    IReadOnlyCollection<HardwareIdentifier> HardwareIds,
    string InstalledDriverVersion,
    bool OfficialSourceOnly,
    bool RequireVerificationEvidence);
