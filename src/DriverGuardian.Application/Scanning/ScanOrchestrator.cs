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

        var discoveryResult = await discoveryService.DiscoverAsync(cancellationToken);
        var distinctDevices = discoveryResult.Devices
            .Distinct(DiscoveredDeviceInstanceIdComparer.Instance)
            .ToArray();

        var issues = new List<ScanIssue>(discoveryResult.Issues);
        DriverInspectionResult inspectionResult;
        if (discoveryResult.Status is DeviceDiscoveryStatus.Failed)
        {
            inspectionResult = new DriverInspectionResult(DriverInspectionStatus.Failed, [], []);
        }
        else if (distinctDevices.Length == 0)
        {
            inspectionResult = new DriverInspectionResult(DriverInspectionStatus.Completed, [], []);
        }
        else
        {
            inspectionResult = await inspectionOrchestrator.InspectAsync(distinctDevices, cancellationToken);
            issues.AddRange(inspectionResult.Issues);
        }

        var completed = session.Complete(clock.UtcNow);
        return new ScanResult(
            completed,
            distinctDevices.Length,
            distinctDevices,
            inspectionResult.Drivers,
            ResolveExecutionStatus(discoveryResult.Status, inspectionResult.Status),
            issues);
    }

    private static ScanExecutionStatus ResolveExecutionStatus(
        DeviceDiscoveryStatus discoveryStatus,
        DriverInspectionStatus inspectionStatus)
    {
        if (discoveryStatus is DeviceDiscoveryStatus.Failed)
        {
            return ScanExecutionStatus.Failed;
        }

        if (inspectionStatus is DriverInspectionStatus.Failed)
        {
            return ScanExecutionStatus.Failed;
        }

        if (discoveryStatus is DeviceDiscoveryStatus.Partial || inspectionStatus is DriverInspectionStatus.Partial)
        {
            return ScanExecutionStatus.Partial;
        }

        return ScanExecutionStatus.Completed;
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
