using DriverGuardian.SystemAdapters.Windows.DeviceDiscovery;
using DriverGuardian.Domain.Settings;

namespace DriverGuardian.Tests.Unit.SystemAdapters.Windows.DeviceDiscovery;

public sealed class WindowsDeviceInclusionPolicyTests
{
    [Fact]
    public void ShouldInclude_ReturnsTrue_ForSystemOrPeripheralClasses()
    {
        var snapshot = new WindowsPnpEntitySnapshot(
            InstanceId: "USB\\VID_046D&PID_C52B\\1",
            FriendlyName: "USB Receiver",
            HardwareIds: ["USB\\VID_046D&PID_C52B"],
            Manufacturer: "Logitech",
            DeviceClass: "HIDClass",
            ConfigManagerErrorCode: 0,
            Status: "OK");

        Assert.True(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot));
    }

    [Fact]
    public void ShouldInclude_ReturnsFalse_ForSoftwareDeviceClass()
    {
        var snapshot = new WindowsPnpEntitySnapshot(
            InstanceId: "SWD\\DRIVERENUM\\{123}",
            FriendlyName: "Software Device",
            HardwareIds: ["SWD\\DRIVERENUM"],
            Manufacturer: "Microsoft",
            DeviceClass: "SoftwareDevice",
            ConfigManagerErrorCode: 0,
            Status: "OK");

        Assert.False(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot));
    }

    [Fact]
    public void ShouldInclude_ReturnsTrue_ForAudioEndpointUnderSwdMmdevapi()
    {
        var snapshot = new WindowsPnpEntitySnapshot(
            InstanceId: "SWD\\MMDEVAPI\\{0.0.1.00000000}",
            FriendlyName: "Microphone",
            HardwareIds: ["HDAUDIO\\FUNC_01"],
            Manufacturer: "Vendor",
            DeviceClass: "AudioEndpoint",
            ConfigManagerErrorCode: 0,
            Status: "OK");

        Assert.True(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot));
    }

    [Fact]
    public void ShouldInclude_ReturnsFalse_InMinimalProfile_ForCameraClass()
    {
        var snapshot = new WindowsPnpEntitySnapshot(
            InstanceId: "USB\\VID_0C45&PID_6A06\\1",
            FriendlyName: "Webcam",
            HardwareIds: ["USB\\VID_0C45&PID_6A06"],
            Manufacturer: "Vendor",
            DeviceClass: "Camera",
            ConfigManagerErrorCode: 0,
            Status: "OK");

        Assert.False(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot, DeviceScanProfile.Minimal));
    }

    [Fact]
    public void ShouldInclude_ReturnsTrue_InComprehensiveProfile_ForNonExcludedClass()
    {
        var snapshot = new WindowsPnpEntitySnapshot(
            InstanceId: "ROOT\\VIRTUALDEVICE\\0001",
            FriendlyName: "Virtual Platform Device",
            HardwareIds: ["ROOT\\VIRTUALDEVICE"],
            Manufacturer: "Vendor",
            DeviceClass: "Platform",
            ConfigManagerErrorCode: 0,
            Status: "OK");

        Assert.True(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot, DeviceScanProfile.Comprehensive));
    }

    [Fact]
    public void ShouldInclude_ReturnsFalse_InBalancedProfile_ForClassOutsideAllowList_EvenWithIncludedHardwarePrefix()
    {
        var snapshot = new WindowsPnpEntitySnapshot(
            InstanceId: "USB\\VID_0000&PID_0000\\1",
            FriendlyName: "USB Camera",
            HardwareIds: ["USB\\VID_0000&PID_0000"],
            Manufacturer: "Vendor",
            DeviceClass: "LegacyUsbCamera",
            ConfigManagerErrorCode: 0,
            Status: "OK");

        Assert.False(WindowsDeviceInclusionPolicy.ShouldInclude(snapshot, DeviceScanProfile.Balanced));
    }
}
