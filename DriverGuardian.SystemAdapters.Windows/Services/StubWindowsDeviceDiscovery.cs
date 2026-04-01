using DriverGuardian.Contracts.Abstractions;
using DriverGuardian.Contracts.Models;
using DriverGuardian.Domain.ValueObjects;

namespace DriverGuardian.SystemAdapters.Windows.Services;

public sealed class StubWindowsDeviceDiscovery : IDeviceDiscovery
{
    public Task<IReadOnlyCollection<DeviceInfo>> DiscoverAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DeviceInfo> result =
        [
            new DeviceInfo(
                new DeviceIdentity("ROOT\\STUB_DEVICE_0001", "Stub Device"),
                [new HardwareIdentifier("PCI\\VEN_0000&DEV_0000")])
        ];

        return Task.FromResult(result);
    }
}
