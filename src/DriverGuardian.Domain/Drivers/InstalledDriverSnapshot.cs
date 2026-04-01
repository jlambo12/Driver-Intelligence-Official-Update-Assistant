using DriverGuardian.Domain.Devices;

namespace DriverGuardian.Domain.Drivers;

public sealed record InstalledDriverSnapshot
{
    public InstalledDriverSnapshot(
        DeviceIdentity deviceIdentity,
        HardwareIdentifier hardwareIdentifier,
        string driverVersion,
        DateOnly? driverDate,
        string? providerName)
    {
        if (string.IsNullOrWhiteSpace(driverVersion))
        {
            throw new ArgumentException("Driver version is required.", nameof(driverVersion));
        }

        DeviceIdentity = deviceIdentity;
        HardwareIdentifier = hardwareIdentifier;
        DriverVersion = driverVersion.Trim();
        DriverDate = driverDate;
        ProviderName = providerName;
    }

    public DeviceIdentity DeviceIdentity { get; }
    public HardwareIdentifier HardwareIdentifier { get; }
    public string DriverVersion { get; }
    public DateOnly? DriverDate { get; }
    public string? ProviderName { get; }
}
