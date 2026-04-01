using DriverGuardian.Contracts.Models;

namespace DriverGuardian.Contracts.Abstractions;

public interface IDriverMetadataInspector
{
    Task<DriverMetadata> InspectAsync(DeviceInfo device, CancellationToken cancellationToken);
}
