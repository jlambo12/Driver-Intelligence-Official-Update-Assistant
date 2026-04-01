namespace DriverGuardian.Contracts.DeviceDiscovery;

public interface IDeviceDiscoveryService
{
    Task<IReadOnlyCollection<DiscoveredDevice>> DiscoverAsync(CancellationToken cancellationToken);
}
