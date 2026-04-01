namespace DriverGuardian.ProviderAdapters.Abstractions.Lookup;

public sealed record ProviderLookupRequest(
    string ProviderCode,
    string DeviceInstanceId,
    IReadOnlyCollection<string> HardwareIds,
    string? InstalledDriverVersion,
    string? OperatingSystemVersion,
    string? DeviceManufacturer,
    string? DeviceModel);
