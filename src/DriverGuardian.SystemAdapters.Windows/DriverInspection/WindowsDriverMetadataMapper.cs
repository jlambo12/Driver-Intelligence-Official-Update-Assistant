using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.SystemAdapters.Windows.DriverInspection;

public static class WindowsDriverMetadataMapper
{
    public static InstalledDriverSnapshot? Map(DiscoveredDevice device, WindowsSignedDriverRecord driverRecord)
    {
        if (string.IsNullOrWhiteSpace(driverRecord.DriverVersion))
        {
            return null;
        }

        var hardwareId = device.HardwareIds.FirstOrDefault();
        if (hardwareId is null)
        {
            return null;
        }

        return new InstalledDriverSnapshot(
            device.Identity,
            hardwareId,
            driverRecord.DriverVersion,
            driverRecord.DriverDate,
            driverRecord.ProviderName);
    }
}
