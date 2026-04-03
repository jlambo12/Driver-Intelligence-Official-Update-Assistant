using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.Contracts.DriverInspection;

public interface IDriverMetadataInspector
{
    Task<DriverInspectionResult> InspectAsync(
        IReadOnlyCollection<DiscoveredDevice> devices,
        CancellationToken cancellationToken);
}

public sealed record DriverInspectionResult(
    DriverInspectionStatus Status,
    IReadOnlyCollection<InstalledDriverSnapshot> Drivers,
    IReadOnlyCollection<ScanIssue> Issues);

public enum DriverInspectionStatus
{
    Completed = 0,
    Partial = 1,
    Failed = 2
}
