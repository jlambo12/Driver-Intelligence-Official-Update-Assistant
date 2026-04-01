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
            new LogMessage(
                Level: AppLogLevel.Debug,
                Category: LogCategory.DeviceDiscovery,
                EventCode: "DEVICE_DISCOVERY_COMPLETED",
                Message: "Device discovery completed.",
                Source: nameof(DriverInspectionOrchestrator),
                OperationContext: context,
                Metadata: new SafeLogMetadata(new Dictionary<string, string> { ["deviceCount"] = devices.Count.ToString() })),
            cancellationToken);

        var snapshots = new List<InstalledDriverSnapshot>(devices.Count);

        foreach (var device in devices)
        {
            var metadata = await metadataInspector.InspectAsync(device, cancellationToken);
            var normalized = normalizationMapper.Map(device, metadata);
            snapshots.Add(normalized.Snapshot);
        }

        await appLogger.LogAsync(
            new LogMessage(
                Level: AppLogLevel.Information,
                Category: LogCategory.DriverInspection,
                EventCode: "DRIVER_INSPECTION_COMPLETED",
                Message: "Driver inspection completed.",
                Source: nameof(DriverInspectionOrchestrator),
                OperationContext: context,
                Metadata: new SafeLogMetadata(new Dictionary<string, string> { ["snapshotCount"] = snapshots.Count.ToString() })),
            cancellationToken);

        return snapshots;
    }
}
