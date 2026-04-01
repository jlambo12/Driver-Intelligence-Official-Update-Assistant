using DriverGuardian.Contracts.DriverInspection;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.SystemAdapters.Windows.DriverInspection;

public sealed class WindowsDriverMetadataInspectorStub : IDriverMetadataInspector
{
    public Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(
        IReadOnlyCollection<DeviceIdentity> deviceIds,
        CancellationToken cancellationToken)
    {
        var snapshots = deviceIds
            .Select(id => new InstalledDriverSnapshot(
                id,
                new HardwareIdentifier("PCI\\VEN_0000&DEV_0000"),
                DriverVersion: "0.0.0-stub",
                DriverDate: null,
                ProviderName: "StubProvider"))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<InstalledDriverSnapshot>>(snapshots);
    }
}
