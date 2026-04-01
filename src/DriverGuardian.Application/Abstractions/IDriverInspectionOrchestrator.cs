using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.Application.Abstractions;

public interface IDriverInspectionOrchestrator
{
    Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(
        IReadOnlyCollection<DiscoveredDevice> devices,
        CancellationToken cancellationToken);
}
