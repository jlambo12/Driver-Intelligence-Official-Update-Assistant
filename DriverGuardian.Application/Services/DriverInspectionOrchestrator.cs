using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Enums;
using DriverGuardian.Application.Logging.Models;
using DriverGuardian.Contracts.Abstractions;
using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Application.Services;

public sealed class DriverInspectionOrchestrator(
    IDeviceDiscovery deviceDiscovery,
    IDriverMetadataInspector metadataInspector,
    IDriverNormalizationMapper normalizationMapper,
    IAppLogger appLogger,
    IOperationContextAccessor operationContextAccessor) : IDriverInspectionOrchestrator
{
    public async Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(CancellationToken cancellationToken)
    {
        var context = operationContextAccessor.Current;
        var devices = await deviceDiscovery.DiscoverAsync(cancellationToken);

        await appLogger.LogAsync(
            AppLogLevel.Debug,
            LogCategory.DeviceDiscovery,
            "DEVICE_DISCOVERY_COMPLETED",
            "Device discovery completed.",
            nameof(DriverInspectionOrchestrator),
            context,
            new SafeLogMetadata(new Dictionary<string, string> { ["deviceCount"] = devices.Count.ToString() }),
            null,
            cancellationToken);

        var snapshots = new List<InstalledDriverSnapshot>(devices.Count);

        foreach (var device in devices)
        {
            var metadata = await metadataInspector.InspectAsync(device, cancellationToken);
            var normalized = normalizationMapper.Map(device, metadata);
            snapshots.Add(normalized.Snapshot);
        }

        await appLogger.LogAsync(
            AppLogLevel.Information,
            LogCategory.DriverInspection,
            "DRIVER_INSPECTION_COMPLETED",
            "Driver inspection completed.",
            nameof(DriverInspectionOrchestrator),
            context,
            new SafeLogMetadata(new Dictionary<string, string> { ["snapshotCount"] = snapshots.Count.ToString() }),
            null,
            cancellationToken);

        return snapshots;
    }
}
