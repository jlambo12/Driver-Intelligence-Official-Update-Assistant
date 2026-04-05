using DriverGuardian.Contracts.DeviceDiscovery;

namespace DriverGuardian.Tests.Unit.Contracts.DeviceDiscovery;

public sealed class DeviceRelevanceClassifierTests
{
    [Fact]
    public void Classify_ReturnsHighPriorityForGpuAndNetwork()
    {
        var gpu = DeviceRelevanceClassifier.Classify("Display", "PCI\\VEN_10DE&DEV_1C8D", ["PCI\\VEN_10DE&DEV_1C8D"], "NVIDIA", "NVIDIA GeForce");
        var wifi = DeviceRelevanceClassifier.Classify("Net", "PCI\\VEN_8086&DEV_2723", ["PCI\\VEN_8086&DEV_2723"], "Intel", "Wi-Fi 6 AX201");
        var bluetooth = DeviceRelevanceClassifier.Classify("Bluetooth", "USB\\VID_0A12&PID_0001", ["USB\\VID_0A12&PID_0001"], "Intel", "Bluetooth Adapter");

        Assert.Equal(DeviceCategory.Gpu, gpu.Category);
        Assert.True(gpu.IsHighPriority);
        Assert.Equal(DeviceCategory.NetworkWifi, wifi.Category);
        Assert.True(wifi.IsHighPriority);
        Assert.Equal(DeviceCategory.Bluetooth, bluetooth.Category);
        Assert.True(bluetooth.IsHighPriority);
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
    public void Classify_DoesNotMarkGenericIntelSystemDeviceAsPlatformCriticalWithoutSignals()
    {
        var platform = DeviceRelevanceClassifier.Classify(
            "System",
            "ROOT\\SYSTEM\\0002",
            ["ROOT\\SYSTEM\\0002"],
            "Intel",
            "Intel Generic System Device");

        Assert.Equal(DeviceCategory.Unknown, platform.Category);
        Assert.False(platform.IsPlatformCritical);
        Assert.False(platform.IsHighPriority);
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

    [Fact]
    public void Classify_UnknownDevice_IsNotAutoRelevant()
    {
        var unknown = DeviceRelevanceClassifier.Classify(
            "System",
            "ROOT\\DEVICE\\0009",
            ["ROOT\\DEVICE\\0009"],
            "Vendor",
            "Generic Device");

        Assert.Equal(DeviceCategory.Unknown, unknown.Category);
        Assert.False(unknown.IsRelevantForUser);
        Assert.False(unknown.IsHighPriority);
    }

    [Fact]
    public void Classify_UsefulAudioEndpoint_IsTreatedAsUserRelevant()
    {
        var audio = DeviceRelevanceClassifier.Classify(
            "AudioEndpoint",
            "SWD\\MMDEVAPI\\{GUID}",
            ["USB\\VID_046D&PID_0A87"],
            "Logitech",
            "USB Conference Headset Microphone");

        Assert.True(audio.IsRelevantForUser);
        Assert.Equal(DeviceCategory.Microphone, audio.Category);
        Assert.False(audio.IsLowValueTechnical);
    }

    [Fact]
    public void Classify_StorageAndUsbController_RemainHighPriority()
    {
        var storage = DeviceRelevanceClassifier.Classify("SCSIAdapter", "PCI\\VEN_8086&DEV_A102", ["PCI\\VEN_8086&DEV_A102"], "Intel", "NVMe Storage Controller");
        var usb = DeviceRelevanceClassifier.Classify("USB", "PCI\\VEN_1022&DEV_43EE", ["PCI\\VEN_1022&DEV_43EE"], "AMD", "USB 3.2 Host Controller");

        Assert.Equal(DeviceCategory.StorageController, storage.Category);
        Assert.True(storage.IsHighPriority);
        Assert.Equal(DeviceCategory.UsbController, usb.Category);
        Assert.True(usb.IsHighPriority);
    }

    [Fact]
    public void Classify_PlantronicsDevice_DoesNotTriggerLanSubstringFalsePositive()
    {
        var device = DeviceRelevanceClassifier.Classify(
            "AudioEndpoint",
            "SWD\\MMDEVAPI\\{PLANTRONICS}",
            ["USB\\VID_047F&PID_C056"],
            "Plantronics",
            "Plantronics USB Audio Device");

        Assert.NotEqual(DeviceCategory.NetworkEthernet, device.Category);
        Assert.NotEqual(DeviceCategory.NetworkWifi, device.Category);
        Assert.NotEqual(DeviceCategory.Bluetooth, device.Category);
    }

    [Fact]
    public void Classify_RealEthernetAndWifi_AreDetectedCorrectly()
    {
        var ethernet = DeviceRelevanceClassifier.Classify("Net", "PCI\\VEN_8086&DEV_15F3", ["PCI\\VEN_8086&DEV_15F3"], "Intel", "Intel Ethernet Network Adapter");
        var wifi = DeviceRelevanceClassifier.Classify("Net", "PCI\\VEN_8086&DEV_2723", ["PCI\\VEN_8086&DEV_2723"], "Intel", "Intel Wireless Wi-Fi Adapter");

        Assert.Equal(DeviceCategory.NetworkEthernet, ethernet.Category);
        Assert.Equal(DeviceCategory.NetworkWifi, wifi.Category);
    }
}
