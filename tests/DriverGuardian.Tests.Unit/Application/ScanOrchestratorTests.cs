using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Scanning;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Contracts.DriverInspection;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class ScanOrchestratorTests
{
    [Fact]
    public async Task RunAsync_ShouldBeCompleted_WhenDiscoveryAndInspectionCompleted()
    {
        var inspector = new FakeDriverMetadataInspector();
        var orchestrator = new ScanOrchestrator(
            new FakeDeviceDiscoveryService(DeviceDiscoveryStatus.Completed, [], "TEST\\DEV\\001"),
            new DriverInspectionOrchestrator(inspector),
            new FakeClock());

        var result = await orchestrator.RunAsync(CancellationToken.None);

        Assert.Equal(ScanExecutionStatus.Completed, result.ExecutionStatus);
        Assert.Single(result.Drivers);
        Assert.Equal(1, inspector.CallCount);
    }

    [Fact]
    public async Task RunAsync_ShouldBePartial_WhenDiscoveryIsPartial()
    {
        var discoveryIssues = new[] { new ScanIssue("discovery", "partial", "partial discovery") };
        var inspector = new FakeDriverMetadataInspector();
        var orchestrator = new ScanOrchestrator(
            new FakeDeviceDiscoveryService(DeviceDiscoveryStatus.Partial, discoveryIssues, "TEST\\DEV\\001"),
            new DriverInspectionOrchestrator(inspector),
            new FakeClock());

        var result = await orchestrator.RunAsync(CancellationToken.None);

        Assert.Equal(ScanExecutionStatus.Partial, result.ExecutionStatus);
        Assert.Single(result.Issues);
        Assert.Equal("discovery", result.Issues.Single().Stage);
    }

    [Fact]
    public async Task RunAsync_ShouldBePartial_WhenInspectionIsPartial()
    {
        var inspectionIssues = new[] { new ScanIssue("inspection", "partial", "partial inspection") };
        var inspector = new FakeDriverMetadataInspector(DriverInspectionStatus.Partial, inspectionIssues);
        var orchestrator = new ScanOrchestrator(
            new FakeDeviceDiscoveryService(DeviceDiscoveryStatus.Completed, [], "TEST\\DEV\\001"),
            new DriverInspectionOrchestrator(inspector),
            new FakeClock());

        var result = await orchestrator.RunAsync(CancellationToken.None);

        Assert.Equal(ScanExecutionStatus.Partial, result.ExecutionStatus);
        Assert.Single(result.Issues);
        Assert.Equal("inspection", result.Issues.Single().Stage);
    }

    [Fact]
    public async Task RunAsync_ShouldBeFailedAndSkipInspection_WhenDiscoveryFailed()
    {
        var inspector = new FakeDriverMetadataInspector();
        var discoveryIssues = new[] { new ScanIssue("discovery", "failed", "wmi failed") };
        var orchestrator = new ScanOrchestrator(
            new FakeDeviceDiscoveryService(DeviceDiscoveryStatus.Failed, discoveryIssues),
            new DriverInspectionOrchestrator(inspector),
            new FakeClock());

        var result = await orchestrator.RunAsync(CancellationToken.None);

        Assert.Equal(ScanExecutionStatus.Failed, result.ExecutionStatus);
        Assert.Equal(0, inspector.CallCount);
        Assert.Single(result.Issues);
        Assert.Equal("discovery", result.Issues.Single().Stage);
    }

    [Fact]
    public async Task RunAsync_ShouldBeFailed_WhenInspectionFailed()
    {
        var inspector = new FakeDriverMetadataInspector(
            DriverInspectionStatus.Failed,
            [new ScanIssue("inspection", "failed", "wmi failed")]);
        var orchestrator = new ScanOrchestrator(
            new FakeDeviceDiscoveryService(DeviceDiscoveryStatus.Completed, [], "TEST\\DEV\\001"),
            new DriverInspectionOrchestrator(inspector),
            new FakeClock());

        var result = await orchestrator.RunAsync(CancellationToken.None);

        Assert.Equal(ScanExecutionStatus.Failed, result.ExecutionStatus);
        Assert.Single(result.Issues);
        Assert.Equal("inspection", result.Issues.Single().Stage);
    }

    [Fact]
    public async Task RunAsync_ShouldAggregateIssues_FromDiscoveryAndInspection()
    {
        var discoveryIssue = new ScanIssue("discovery", "partial", "partial discovery");
        var inspectionIssue = new ScanIssue("inspection", "partial", "partial inspection");
        var inspector = new FakeDriverMetadataInspector(DriverInspectionStatus.Partial, [inspectionIssue]);
        var orchestrator = new ScanOrchestrator(
            new FakeDeviceDiscoveryService(DeviceDiscoveryStatus.Partial, [discoveryIssue], "TEST\\DEV\\001"),
            new DriverInspectionOrchestrator(inspector),
            new FakeClock());

        var result = await orchestrator.RunAsync(CancellationToken.None);

        Assert.Equal(2, result.Issues.Count);
        Assert.Contains(result.Issues, issue => issue == discoveryIssue);
        Assert.Contains(result.Issues, issue => issue == inspectionIssue);
    }

    [Fact]
    public async Task RunAsync_ShouldKeepCompleted_WhenNoDevicesAndDiscoveryCompleted()
    {
        var inspector = new FakeDriverMetadataInspector();
        var orchestrator = new ScanOrchestrator(
            new FakeDeviceDiscoveryService(DeviceDiscoveryStatus.Completed, []),
            new DriverInspectionOrchestrator(inspector),
            new FakeClock());

        var result = await orchestrator.RunAsync(CancellationToken.None);

        Assert.Empty(result.Drivers);
        Assert.Equal(ScanExecutionStatus.Completed, result.ExecutionStatus);
        Assert.Equal(0, inspector.CallCount);
    }

    [Fact]
    public async Task RunAsync_ShouldKeepPartial_WhenNoDevicesAndDiscoveryPartial()
    {
        var inspector = new FakeDriverMetadataInspector();
        var orchestrator = new ScanOrchestrator(
            new FakeDeviceDiscoveryService(DeviceDiscoveryStatus.Partial, [new ScanIssue("discovery", "partial", "partial discovery")]),
            new DriverInspectionOrchestrator(inspector),
            new FakeClock());

        var result = await orchestrator.RunAsync(CancellationToken.None);

        Assert.Empty(result.Drivers);
        Assert.Equal(ScanExecutionStatus.Partial, result.ExecutionStatus);
        Assert.Equal(0, inspector.CallCount);
    }

    [Fact]
    public async Task RunAsync_ShouldDeduplicateDeviceIdentitiesBeforeInspection()
    {
        var inspector = new FakeDriverMetadataInspector();
        var orchestrator = new ScanOrchestrator(
            new FakeDeviceDiscoveryService(DeviceDiscoveryStatus.Completed, [], "TEST\\DEV\\001", "test\\dev\\001", "TEST\\DEV\\002"),
            new DriverInspectionOrchestrator(inspector),
            new FakeClock());

        await orchestrator.RunAsync(CancellationToken.None);

        Assert.Equal(1, inspector.CallCount);
        Assert.Equal(2, inspector.LastRequestedDevices.Count);
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeDeviceDiscoveryService(
        DeviceDiscoveryStatus status,
        IReadOnlyCollection<ScanIssue> issues,
        params string[] instanceIds) : IDeviceDiscoveryService
    {
        private readonly string[] _instanceIds = instanceIds;

        public Task<DeviceDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken)
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

            return Task.FromResult(new DeviceDiscoveryResult(status, devices, issues));
        }
    }

    private sealed class FakeDriverMetadataInspector(
        DriverInspectionStatus status = DriverInspectionStatus.Completed,
        IReadOnlyCollection<ScanIssue>? issues = null) : IDriverMetadataInspector
    {
        public int CallCount { get; private set; }

        public IReadOnlyCollection<DiscoveredDevice> LastRequestedDevices { get; private set; } = [];

        private readonly IReadOnlyCollection<ScanIssue> _issues = issues ?? [];

        public Task<DriverInspectionResult> InspectAsync(
            IReadOnlyCollection<DiscoveredDevice> devices,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequestedDevices = devices.ToArray();

            var firstDevice = devices.FirstOrDefault();
            if (firstDevice is null)
            {
                return Task.FromResult(new DriverInspectionResult(status, [], _issues));
            }

            IReadOnlyCollection<InstalledDriverSnapshot> snapshots =
            [
                new InstalledDriverSnapshot(
                    firstDevice.Identity,
                    firstDevice.HardwareIds.First(),
                    "1.0.0",
                    null,
                    "Fake")
            ];

            return Task.FromResult(new DriverInspectionResult(status, snapshots, _issues));
        }
    }
}
