using DriverGuardian.Contracts.Models;

namespace DriverGuardian.Contracts.Abstractions;

public interface IDeviceDiscovery
{
    Task<IReadOnlyCollection<DeviceInfo>> DiscoverAsync(CancellationToken cancellationToken);
}
