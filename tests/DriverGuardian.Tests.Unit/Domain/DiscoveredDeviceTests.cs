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

    [Fact]
    public void Create_ShouldNormalizeOptionalDiscoveryMetadata()
    {
        var device = DiscoveredDevice.Create(
            instanceId: "USB\\VID_1234&PID_5678",
            displayName: "  Demo Device  ",
            hardwareIds: [" ", "USB\\VID_1234&PID_5678  "],
            manufacturer: "  Fabrikam  ",
            deviceClass: "  USB  ",
            presenceStatus: DevicePresenceStatus.Present,
            rawStatus: "  OK  ");

        Assert.Equal("Demo Device", device.DisplayName);
        Assert.Single(device.HardwareIds);
        Assert.Equal("USB\\VID_1234&PID_5678", device.HardwareIds.Single().Value);
        Assert.Equal("Fabrikam", device.Manufacturer);
        Assert.Equal("USB", device.DeviceClass);
        Assert.Equal("OK", device.RawStatus);
    }
}
