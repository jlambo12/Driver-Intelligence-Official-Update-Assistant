using DriverGuardian.Application.Abstractions;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Contracts.DriverInspection;

namespace DriverGuardian.Application.Scanning;

public sealed class DriverInspectionOrchestrator(IDriverMetadataInspector inspector) : IDriverInspectionOrchestrator
{
    public Task<DriverInspectionResult> InspectAsync(
        IReadOnlyCollection<DiscoveredDevice> devices,
        CancellationToken cancellationToken)
    {
        return inspector.InspectAsync(devices, cancellationToken);
    }
}
