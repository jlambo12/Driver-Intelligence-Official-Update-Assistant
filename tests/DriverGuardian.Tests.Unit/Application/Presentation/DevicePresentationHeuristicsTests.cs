using DriverGuardian.Application.Presentation;
using DriverGuardian.Contracts.DeviceDiscovery;

namespace DriverGuardian.Tests.Unit.Application.Presentation;

public sealed class DevicePresentationHeuristicsTests
{
    private const string GenericSyntheticHardwareId = "SYNTH\\HWID\\GENERIC";

    [Theory]
    [InlineData("Keyboard", "USB\\VID_0001", "USB Keyboard")]
    [InlineData("Mouse", "USB\\VID_0002", "USB Mouse")]
    [InlineData("Monitor", "DISPLAY\\ACME123", "Generic PnP Monitor")]
    [InlineData("Printer", "USB\\VID_0003", "Office Printer")]
    [InlineData("Camera", "USB\\VID_0004", "HD Webcam")]
    [InlineData("Media", "HDAUDIO\\FUNC_01", "Microphone Array")]
    public void IsUserRelevant_ReturnsTrue_ForUserPeripheralClasses(string deviceClass, string instanceId, string name)
    {
        var device = BuildDevice(instanceId, name, deviceClass);

        Assert.True(DevicePresentationHeuristics.IsUserRelevant(device, hasRecommendation: false));
    }

    [Fact]
    public void ResolvePriorityBucket_DoesNotTreatAudioEndpointAsTopPriority()
    {
        var endpoint = BuildDevice(
            "SWD\\MMDEVAPI\\{FAKE}",
            "SWD\\MMDEVAPI\\{FAKE}",
            "AudioEndpoint",
            ["ROOT\\MMDEVAPI"]);

        var bucket = DevicePresentationHeuristics.ResolvePriorityBucket(endpoint, hasRecommendation: false);

        Assert.NotEqual(0, bucket);
        Assert.NotEqual(1, bucket);
    }

    [Fact]
    public void IsUserRelevant_ReturnsFalse_ForLowValueRootEnumeratorNoise()
    {
        var device = BuildDevice("ROOT\\SYSTEM\\0001", "Virtual Enumerator Device", "System");

        Assert.False(DevicePresentationHeuristics.IsUserRelevant(device, hasRecommendation: false));
    }

    [Fact]
    public void IsUserRelevant_AlwaysReturnsTrue_WhenRecommendationExists()
    {
        var device = BuildDevice("SWD\\MMDEVAPI\\{FAKE}", "Endpoint", "AudioEndpoint");

        Assert.True(DevicePresentationHeuristics.IsUserRelevant(device, hasRecommendation: true));
        Assert.Equal(0, DevicePresentationHeuristics.ResolvePriorityBucket(device, hasRecommendation: true));
    }

    [Fact]
    public void IsUserRelevant_KeepsUsefulAudioPeripheral()
    {
        var device = BuildDevice("SWD\\MMDEVAPI\\{USBHEADSET}", "USB Conference Headset Microphone", "AudioEndpoint");

        Assert.True(DevicePresentationHeuristics.IsUserRelevant(device, hasRecommendation: false));
        Assert.Equal(2, DevicePresentationHeuristics.ResolvePriorityBucket(device, hasRecommendation: false));
    }

    private static DiscoveredDevice BuildDevice(string instanceId, string displayName, string deviceClass, IReadOnlyCollection<string>? hardwareIds = null)
        => DiscoveredDevice.Create(
            instanceId,
            displayName,
            hardwareIds ?? [GenericSyntheticHardwareId],
            "Vendor",
            deviceClass,
            DevicePresenceStatus.Present,
            "OK");
}
