using DriverGuardian.Application.Abstractions;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Scanning;

namespace DriverGuardian.Application.Scanning;

public sealed class ScanOrchestrator(
    IDeviceDiscoveryService discoveryService,
    IDriverInspectionOrchestrator inspectionOrchestrator,
    IClock clock) : IScanOrchestrator
{
    public async Task<ScanResult> RunAsync(CancellationToken cancellationToken)
    {
        var started = clock.UtcNow;
        var session = ScanSession.Start(Guid.NewGuid(), started);

        var discoveredDevices = await discoveryService.DiscoverAsync(cancellationToken);
        var distinctDevices = discoveredDevices
            .Distinct(DiscoveredDeviceInstanceIdComparer.Instance)
            .ToArray();

        var drivers = distinctDevices.Length == 0
            ? []
            : await inspectionOrchestrator.InspectAsync(distinctDevices, cancellationToken);

        var completed = session.Complete(clock.UtcNow);
        return new ScanResult(completed, distinctDevices.Length, distinctDevices, drivers);
    }

    private sealed class DiscoveredDeviceInstanceIdComparer : IEqualityComparer<DiscoveredDevice>
    {
        public static DiscoveredDeviceInstanceIdComparer Instance { get; } = new();

        public bool Equals(DiscoveredDevice? x, DiscoveredDevice? y)
        {
            return DeviceIdentityInstanceIdComparer.Instance.Equals(x?.Identity, y?.Identity);
        }

        public int GetHashCode(DiscoveredDevice obj)
        {
            return DeviceIdentityInstanceIdComparer.Instance.GetHashCode(obj.Identity);
        }
    }

    private sealed class DeviceIdentityInstanceIdComparer : IEqualityComparer<DeviceIdentity>
    {
        public static DeviceIdentityInstanceIdComparer Instance { get; } = new();

        public bool Equals(DeviceIdentity? x, DeviceIdentity? y)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(x?.InstanceId, y?.InstanceId);
        }

        public int GetHashCode(DeviceIdentity obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.InstanceId);
        }
    }
}
