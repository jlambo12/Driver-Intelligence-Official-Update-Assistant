using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.Contracts.DriverInspection;

public interface IDriverMetadataInspector
{
    Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(
        IReadOnlyCollection<DeviceIdentity> deviceIds,
        CancellationToken cancellationToken);
}
