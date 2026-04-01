using DriverGuardian.Application.Abstractions;
using DriverGuardian.Contracts.Abstractions;
using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Application.Services;

public sealed class DriverInspectionOrchestrator(
    IDeviceDiscovery deviceDiscovery,
    IDriverMetadataInspector metadataInspector,
    IDriverNormalizationMapper normalizationMapper) : IDriverInspectionOrchestrator
{
    public async Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(CancellationToken cancellationToken)
    {
        var devices = await deviceDiscovery.DiscoverAsync(cancellationToken);
        var snapshots = new List<InstalledDriverSnapshot>(devices.Count);

        foreach (var device in devices)
        {
            var metadata = await metadataInspector.InspectAsync(device, cancellationToken);
            var normalized = normalizationMapper.Map(device, metadata);
            snapshots.Add(normalized.Snapshot);
        }

        return snapshots;
    }
}
