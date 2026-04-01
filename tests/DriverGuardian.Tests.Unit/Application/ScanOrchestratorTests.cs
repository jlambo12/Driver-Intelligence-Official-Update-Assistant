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
        var inspector = new FakeDriverMetadataInspector();
        var orchestrator = new ScanOrchestrator(
            new FakeDeviceDiscoveryService("TEST\\DEV\\001"),
            new DriverInspectionOrchestrator(inspector),
            new FakeClock());

        var result = await orchestrator.RunAsync(CancellationToken.None);

        Assert.Single(result.Drivers);
        Assert.NotNull(result.Session.CompletedAtUtc);
        Assert.Equal(1, inspector.CallCount);
    }

    [Fact]
    public async Task RunAsync_ShouldSkipDriverInspection_WhenNoDevicesWereDiscovered()
    {
        var inspector = new FakeDriverMetadataInspector();
        var orchestrator = new ScanOrchestrator(
            new FakeDeviceDiscoveryService(),
            new DriverInspectionOrchestrator(inspector),
            new FakeClock());

        var result = await orchestrator.RunAsync(CancellationToken.None);

        Assert.Empty(result.Drivers);
        Assert.Equal(0, inspector.CallCount);
    }

    [Fact]
    public async Task RunAsync_ShouldDeduplicateDeviceIdentitiesBeforeInspection()
    {
        var inspector = new FakeDriverMetadataInspector();
        var orchestrator = new ScanOrchestrator(
            new FakeDeviceDiscoveryService("TEST\\DEV\\001", "test\\dev\\001", "TEST\\DEV\\002"),
            new DriverInspectionOrchestrator(inspector),
            new FakeClock());

        await orchestrator.RunAsync(CancellationToken.None);

        Assert.Equal(1, inspector.CallCount);
        Assert.Equal(2, inspector.LastRequestedDeviceIds.Count);
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeDeviceDiscoveryService(params string[] instanceIds) : IDeviceDiscoveryService
    {
        private readonly string[] _instanceIds = instanceIds;

        public Task<IReadOnlyCollection<DiscoveredDevice>> DiscoverAsync(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<DiscoveredDevice> devices = _instanceIds
                .Select(id => DiscoveredDevice.Create(
                    instanceId: id,
                    displayName: "Test",
                    hardwareIds: ["PCI\\VEN_1234&DEV_ABCD"],
                    manufacturer: "Vendor",
                    deviceClass: "System",
                    presenceStatus: DevicePresenceStatus.Present,
                    rawStatus: "OK"))
                .ToArray();

            return Task.FromResult(devices);
        }
    }

    private sealed class FakeDriverMetadataInspector : IDriverMetadataInspector
    {
        public int CallCount { get; private set; }

        public IReadOnlyCollection<DeviceIdentity> LastRequestedDeviceIds { get; private set; } = [];

        public Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(
            IReadOnlyCollection<DeviceIdentity> deviceIds,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequestedDeviceIds = deviceIds.ToArray();

            if (deviceIds.Count == 0)
            {
                return Task.FromResult<IReadOnlyCollection<InstalledDriverSnapshot>>([]);
            }

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
