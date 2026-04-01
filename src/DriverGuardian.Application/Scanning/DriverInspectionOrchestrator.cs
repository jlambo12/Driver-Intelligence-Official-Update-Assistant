using DriverGuardian.Application.Abstractions;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Contracts.DriverInspection;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.Application.Scanning;

public sealed class DriverInspectionOrchestrator(IDriverMetadataInspector inspector) : IDriverInspectionOrchestrator
{
    public Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(
        IReadOnlyCollection<DiscoveredDevice> devices,
        CancellationToken cancellationToken)
    {
        return inspector.InspectAsync(devices, cancellationToken);
    }
}
