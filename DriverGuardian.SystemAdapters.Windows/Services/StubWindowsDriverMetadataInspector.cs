using DriverGuardian.Contracts.Abstractions;
using DriverGuardian.Contracts.Models;

namespace DriverGuardian.SystemAdapters.Windows.Services;

public sealed class StubWindowsDriverMetadataInspector : IDriverMetadataInspector
{
    public Task<DriverMetadata> InspectAsync(DeviceInfo device, CancellationToken cancellationToken)
        => Task.FromResult(new DriverMetadata("0.0.0-stub", null, "Unknown", false, null));
}
