using DriverGuardian.Contracts.DeviceDiscovery;

namespace DriverGuardian.Tests.Unit.Contracts.DeviceDiscovery;

public sealed class DeviceRelevanceClassifierTests
{
    [Fact]
    public void Classify_ReturnsHighPriorityForGpuAndNetwork()
    {
        var gpu = DeviceRelevanceClassifier.Classify("Display", "PCI\\VEN_10DE&DEV_1C8D", ["PCI\\VEN_10DE&DEV_1C8D"], "NVIDIA", "NVIDIA GeForce");
        var wifi = DeviceRelevanceClassifier.Classify("Net", "PCI\\VEN_8086&DEV_2723", ["PCI\\VEN_8086&DEV_2723"], "Intel", "Wi-Fi 6 AX201");

        Assert.Equal(DeviceCategory.Gpu, gpu.Category);
        Assert.True(gpu.IsHighPriority);
        Assert.Equal(DeviceCategory.NetworkWifi, wifi.Category);
        Assert.True(wifi.IsHighPriority);
    }

    [Fact]
    public void Classify_RecognizesPlatformCriticalSystemDevice()
    {
        var platform = DeviceRelevanceClassifier.Classify(
            "System",
            "PCI\\VEN_8086&DEV_A36D",
            ["PCI\\VEN_8086&DEV_A36D"],
            "Intel",
            "Intel SMBus Controller");

        Assert.Equal(DeviceCategory.MotherboardPlatform, platform.Category);
        Assert.True(platform.IsPlatformCritical);
    }

    [Fact]
    public void Classify_FlagsSoftwareAndSwdNoiseAsLowValue()
    {
        var software = DeviceRelevanceClassifier.Classify("SoftwareComponent", "SWD\\DRIVERENUM\\{123}", ["SWD\\DRIVERENUM"], "Microsoft", "Software Component");
        var rootNoise = DeviceRelevanceClassifier.Classify("System", "ROOT\\SYSTEM\\0001", ["ROOT\\SYSTEM"], "Microsoft", "Virtual Enumerator Device");

        Assert.Equal(DeviceCategory.VirtualOrSoftware, software.Category);
        Assert.False(software.IsRelevantForUser);
        Assert.Equal(DeviceCategory.VirtualOrSoftware, rootNoise.Category);
        Assert.True(rootNoise.IsLowValueTechnical);
    }
}
