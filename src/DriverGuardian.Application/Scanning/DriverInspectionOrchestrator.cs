using DriverGuardian.Application.Abstractions;
using DriverGuardian.Contracts.DriverInspection;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.Application.Scanning;

public sealed class DriverInspectionOrchestrator(IDriverMetadataInspector inspector) : IDriverInspectionOrchestrator
{
    public Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(
        IReadOnlyCollection<DeviceIdentity> deviceIds,
        CancellationToken cancellationToken)
    {
        return inspector.InspectAsync(deviceIds, cancellationToken);
    }
}
