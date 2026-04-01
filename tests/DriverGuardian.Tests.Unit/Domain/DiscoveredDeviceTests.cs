using DriverGuardian.Contracts.DeviceDiscovery;

namespace DriverGuardian.Tests.Unit.Domain;

public sealed class DiscoveredDeviceTests
{
    [Fact]
    public void Create_ShouldFallbackDisplayNameToInstanceId_WhenNameIsMissing()
    {
        var device = DiscoveredDevice.Create(
            instanceId: "PCI\\ROOT\\0001",
            displayName: " ",
            hardwareIds: ["pci\\ven_1234&dev_abcd", "PCI\\VEN_1234&DEV_ABCD"],
            manufacturer: "  ",
            deviceClass: null,
            presenceStatus: DevicePresenceStatus.Unknown,
            rawStatus: null);

        Assert.Equal("PCI\\ROOT\\0001", device.DisplayName);
        Assert.Single(device.HardwareIds);
        Assert.Null(device.Manufacturer);
        Assert.Equal(DevicePresenceStatus.Unknown, device.PresenceStatus);
    }
}
