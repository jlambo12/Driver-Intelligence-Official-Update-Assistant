using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.SystemAdapters.Windows.DriverInspection;

namespace DriverGuardian.Tests.Unit.SystemAdapters.Windows.DriverInspection;

public sealed class WindowsDriverMetadataMapperTests
{
    [Fact]
    public void Map_ShouldReturnSnapshot_WhenDriverRecordContainsRequiredFields()
    {
        var device = DiscoveredDevice.Create(
            instanceId: "PCI\\VEN_1234&DEV_ABCD\\1",
            displayName: "GPU",
            hardwareIds: ["PCI\\VEN_1234&DEV_ABCD"],
            manufacturer: "Vendor",
            deviceClass: "Display",
            presenceStatus: DevicePresenceStatus.Present,
            rawStatus: "OK");

        var record = new WindowsSignedDriverRecord(
            InstanceId: device.Identity.InstanceId,
            DriverVersion: "31.0.15.3713",
            DriverDate: new DateOnly(2025, 10, 5),
            ProviderName: "NVIDIA");

        var snapshot = WindowsDriverMetadataMapper.Map(device, record);

        Assert.NotNull(snapshot);
        Assert.Equal("31.0.15.3713", snapshot!.DriverVersion);
        Assert.Equal(new DateOnly(2025, 10, 5), snapshot.DriverDate);
        Assert.Equal("NVIDIA", snapshot.ProviderName);
        Assert.Equal(device.Identity, snapshot.DeviceIdentity);
        Assert.Equal(device.HardwareIds.First(), snapshot.HardwareIdentifier);
    }

    [Fact]
    public void Map_ShouldReturnNull_WhenDriverVersionIsMissing()
    {
        var device = DiscoveredDevice.Create(
            instanceId: "PCI\\VEN_1234&DEV_ABCD\\1",
            displayName: "GPU",
            hardwareIds: ["PCI\\VEN_1234&DEV_ABCD"],
            manufacturer: "Vendor",
            deviceClass: "Display",
            presenceStatus: DevicePresenceStatus.Present,
            rawStatus: "OK");

        var record = new WindowsSignedDriverRecord(
            InstanceId: device.Identity.InstanceId,
            DriverVersion: null,
            DriverDate: null,
            ProviderName: "NVIDIA");

        var snapshot = WindowsDriverMetadataMapper.Map(device, record);

        Assert.Null(snapshot);
    }

    [Fact]
    public void Map_ShouldReturnNull_WhenHardwareIdentifierIsMissingOnDevice()
    {
        var device = DiscoveredDevice.Create(
            instanceId: "PCI\\VEN_1234&DEV_ABCD\\1",
            displayName: "GPU",
            hardwareIds: [],
            manufacturer: "Vendor",
            deviceClass: "Display",
            presenceStatus: DevicePresenceStatus.Present,
            rawStatus: "OK");

        var record = new WindowsSignedDriverRecord(
            InstanceId: device.Identity.InstanceId,
            DriverVersion: "31.0.15.3713",
            DriverDate: null,
            ProviderName: "NVIDIA");

        var snapshot = WindowsDriverMetadataMapper.Map(device, record);

        Assert.Null(snapshot);
    }
}
