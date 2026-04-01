using DriverGuardian.Domain.ValueObjects;

namespace DriverGuardian.Contracts.Models;

public sealed record DeviceInfo(DeviceIdentity Identity, IReadOnlyCollection<HardwareIdentifier> HardwareIds);
