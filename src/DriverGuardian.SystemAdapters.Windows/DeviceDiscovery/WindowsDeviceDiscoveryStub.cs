using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Domain.Devices;

namespace DriverGuardian.SystemAdapters.Windows.DeviceDiscovery;

public sealed class WindowsDeviceDiscoveryStub : IDeviceDiscoveryService
{
    public Task<IReadOnlyCollection<DiscoveredDevice>> DiscoverAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DiscoveredDevice> devices =
        [
            new DiscoveredDevice(
                new DeviceIdentity("STUB\\DEVICE\\0001"),
                "Stub Device",
                [new HardwareIdentifier("PCI\\VEN_0000&DEV_0000")])
        ];

        return Task.FromResult(devices);
    }
}
