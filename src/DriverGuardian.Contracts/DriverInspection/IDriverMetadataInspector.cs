using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.Contracts.DriverInspection;

public interface IDriverMetadataInspector
{
    Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(
        IReadOnlyCollection<DiscoveredDevice> devices,
        CancellationToken cancellationToken);
}
