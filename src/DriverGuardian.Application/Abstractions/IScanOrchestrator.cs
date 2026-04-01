using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Scanning;

namespace DriverGuardian.Application.Abstractions;

public interface IScanOrchestrator
{
    Task<ScanResult> RunAsync(CancellationToken cancellationToken);
}

public sealed record ScanResult(
    ScanSession Session,
    int DiscoveredDeviceCount,
    IReadOnlyCollection<InstalledDriverSnapshot> Drivers);
