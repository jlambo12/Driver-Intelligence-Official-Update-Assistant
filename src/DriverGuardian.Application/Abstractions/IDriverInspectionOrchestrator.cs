using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Contracts.DriverInspection;

namespace DriverGuardian.Application.Abstractions;

public interface IDriverInspectionOrchestrator
{
    Task<DriverInspectionResult> InspectAsync(
        IReadOnlyCollection<DiscoveredDevice> devices,
        CancellationToken cancellationToken);
}
