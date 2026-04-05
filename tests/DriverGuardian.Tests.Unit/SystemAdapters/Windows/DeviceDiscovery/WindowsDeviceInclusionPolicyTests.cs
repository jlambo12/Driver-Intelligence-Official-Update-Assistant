using DriverGuardian.SystemAdapters.Windows.DeviceDiscovery;
using DriverGuardian.Domain.Settings;

namespace DriverGuardian.Tests.Unit.SystemAdapters.Windows.DeviceDiscovery;

public sealed class WindowsDeviceInclusionPolicyTests
{
    [Fact]
    public void ShouldInclude_ReturnsTrue_ForGpuDevice()
    {
        var snapshot = BuildSnapshot("PCI\\VEN_10DE&DEV_1C8D", "NVIDIA GeForce", "Display", ["PCI\\VEN_10DE&DEV_1C8D"]);

        Assert.True(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot));
        Assert.True(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot, DeviceScanProfile.Minimal));
    }

    [Fact]
    public void ShouldInclude_ReturnsTrue_ForEthernetAndWifiDevices()
    {
        var ethernet = BuildSnapshot("PCI\\VEN_8086&DEV_15F3", "Intel Ethernet Controller", "Net", ["PCI\\VEN_8086&DEV_15F3"]);
        var wifi = BuildSnapshot("PCI\\VEN_8086&DEV_2723", "Intel Wi-Fi 6 AX201", "Net", ["PCI\\VEN_8086&DEV_2723"]);

        Assert.True(WindowsDeviceInclusionPolicy.ShouldInclude(ethernet));
        Assert.True(WindowsDeviceInclusionPolicy.ShouldInclude(wifi));
    }

    [Fact]
    public void ShouldInclude_KeepsPlatformCriticalSystemEntries()
    {
        var snapshot = BuildSnapshot(
            "PCI\\VEN_8086&DEV_A36D",
            "Intel(R) SMBus - A36D",
            "System",
            ["PCI\\VEN_8086&DEV_A36D"]);

        Assert.True(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot));
        Assert.True(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot, DeviceScanProfile.Minimal));
    }

    [Fact]
    public void ShouldInclude_ReturnsFalse_ForSoftwareDeviceClass()
    {
        var snapshot = BuildSnapshot("SWD\\DRIVERENUM\\{123}", "Software Device", "SoftwareDevice", ["SWD\\DRIVERENUM"]);

        Assert.False(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot));
    }

    [Fact]
    public void ShouldInclude_ReturnsFalse_ForAudioEndpointInMinimalProfile()
    {
        var snapshot = BuildSnapshot(
            "SWD\\MMDEVAPI\\{0.0.1.00000000}",
            "Microphone",
            "AudioEndpoint",
            ["HDAUDIO\\FUNC_01"]);

        Assert.False(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot, DeviceScanProfile.Minimal));
    }

    [Fact]
    public void ShouldInclude_ReturnsTrue_InBalancedProfile_ForCameraClass()
    {
        var snapshot = BuildSnapshot("USB\\VID_0C45&PID_6A06\\1", "Webcam", "Camera", ["USB\\VID_0C45&PID_6A06"]);

        Assert.True(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot, DeviceScanProfile.Balanced));
    }

    [Fact]
    public void ShouldInclude_ReturnsTrue_InComprehensiveProfile_ForNonExcludedClass()
    {
        var snapshot = BuildSnapshot("ROOT\\VIRTUALDEVICE\\0001", "Virtual Platform Device", "Platform", ["ROOT\\VIRTUALDEVICE"]);

        Assert.True(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot, DeviceScanProfile.Comprehensive));
    }

    [Fact]
    public void ShouldInclude_UnknownDevice_IsExcludedInMinimalButKeptInBalanced()
    {
        var snapshot = BuildSnapshot("ROOT\\DEVICE\\0010", "Generic Device", "System", ["ROOT\\DEVICE\\0010"]);

        Assert.False(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot, DeviceScanProfile.Minimal));
        Assert.True(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot, DeviceScanProfile.Balanced));
    }

    private static WindowsPnpEntitySnapshot BuildSnapshot(string instanceId, string friendlyName, string deviceClass, IReadOnlyCollection<string> hardwareIds)
        => new(
            InstanceId: instanceId,
            FriendlyName: friendlyName,
            HardwareIds: hardwareIds,
            Manufacturer: "Vendor",
            DeviceClass: deviceClass,
            ConfigManagerErrorCode: 0,
            Status: "OK");
}
