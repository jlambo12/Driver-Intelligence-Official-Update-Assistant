using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Scanning;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Contracts.DriverInspection;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class ScanOrchestratorTests
{
    [Fact]
    public async Task RunAsync_ShouldReturnScannedDrivers()
    {
        var orchestrator = new ScanOrchestrator(
            new FakeDeviceDiscoveryService(),
            new DriverInspectionOrchestrator(new FakeDriverMetadataInspector()),
            new FakeClock());

        var result = await orchestrator.RunAsync(CancellationToken.None);

        Assert.Single(result.Drivers);
        Assert.NotNull(result.Session.CompletedAtUtc);
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeDeviceDiscoveryService : IDeviceDiscoveryService
    {
        public Task<IReadOnlyCollection<DiscoveredDevice>> DiscoverAsync(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<DiscoveredDevice> devices =
            [
                new DiscoveredDevice(new DeviceIdentity("TEST\\DEV\\001"), "Test", [new HardwareIdentifier("PCI\\VEN_1234&DEV_ABCD")])
            ];

            return Task.FromResult(devices);
        }
    }

    private sealed class FakeDriverMetadataInspector : IDriverMetadataInspector
    {
        public Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(
            IReadOnlyCollection<DeviceIdentity> deviceIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<InstalledDriverSnapshot> snapshots =
            [
                new InstalledDriverSnapshot(
                    deviceIds.First(),
                    new HardwareIdentifier("PCI\\VEN_1234&DEV_ABCD"),
                    "1.0.0",
                    null,
                    "Fake")
            ];

            return Task.FromResult(snapshots);
        }
    }
}
