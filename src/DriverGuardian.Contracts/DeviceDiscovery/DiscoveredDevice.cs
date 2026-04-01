using DriverGuardian.Domain.Devices;

namespace DriverGuardian.Contracts.DeviceDiscovery;

public sealed record DiscoveredDevice(DeviceIdentity Identity, string DisplayName, IReadOnlyCollection<HardwareIdentifier> HardwareIds);
