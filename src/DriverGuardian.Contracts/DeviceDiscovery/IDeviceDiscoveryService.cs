namespace DriverGuardian.Contracts.DeviceDiscovery;

public interface IDeviceDiscoveryService
{
    Task<DeviceDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken);
}

public sealed record DeviceDiscoveryResult(
    DeviceDiscoveryStatus Status,
    IReadOnlyCollection<DiscoveredDevice> Devices,
    IReadOnlyCollection<ScanIssue> Issues);

public enum DeviceDiscoveryStatus
{
    Completed = 0,
    Partial = 1,
    Failed = 2
}

public sealed record ScanIssue(
    string Stage,
    string Code,
    string Message);
