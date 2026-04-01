using DriverGuardian.Application.Abstractions;
using DriverGuardian.Contracts.DeviceDiscovery;
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

        var devices = await discoveryService.DiscoverAsync(cancellationToken);
        var drivers = await inspectionOrchestrator.InspectAsync(devices.Select(d => d.Identity).ToArray(), cancellationToken);

        var completed = session.Complete(clock.UtcNow);
        return new ScanResult(completed, drivers);
    }
}
