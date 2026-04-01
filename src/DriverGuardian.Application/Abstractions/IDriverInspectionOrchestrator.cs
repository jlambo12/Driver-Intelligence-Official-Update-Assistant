using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.Application.Abstractions;

public interface IDriverInspectionOrchestrator
{
    Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(
        IReadOnlyCollection<DeviceIdentity> deviceIds,
        CancellationToken cancellationToken);
}
