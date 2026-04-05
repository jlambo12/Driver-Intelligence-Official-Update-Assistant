namespace DriverGuardian.Contracts.DeviceDiscovery;

public enum DeviceCategory
{
    Unknown = 0,
    Chipset = 1,
    MotherboardPlatform = 2,
    StorageController = 3,
    UsbController = 4,
    Gpu = 5,
    NetworkEthernet = 6,
    NetworkWifi = 7,
    Bluetooth = 8,
    Audio = 9,
    Monitor = 10,
    Keyboard = 11,
    Mouse = 12,
    Printer = 13,
    Scanner = 14,
    Microphone = 15,
    Camera = 16,
    HidPeripheral = 17,
    DockingStation = 18,
    Touchpad = 19,
    CardReader = 20,
    Biometric = 21,
    LowValueTechnical = 22,
    VirtualOrSoftware = 23
}

public sealed record DeviceClassification(
    DeviceCategory Category,
    bool IsRelevantForUser,
    bool IsHighPriority,
    bool IsLowValueTechnical,
    bool IsVirtualOrSoftware,
    bool IsPlatformCritical,
    bool IsRecommendationScopeCandidate);

public static class DeviceRelevanceClassifier
{
    private static readonly HashSet<string> SoftwareClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "SoftwareDevice",
        "SoftwareComponent",
        "LegacyDriver"
    };

    private static readonly HashSet<string> LowValueClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "PrintQueue",
        "AudioEndpoint"
    };

    private static readonly string[] LowValueInstancePrefixes =
    [
        "SWD\\",
        "MMDEVAPI\\",
        "PRINTENUM\\"
    ];

    private static readonly string[] RootLowValueNameKeywords =
    [
        "enumerator",
        "virtual",
        "composite",
        "proxy",
        "bridge"
    ];

    private static readonly string[] PlatformKeywords =
    [
        "chipset",
        "smbus",
        "acpi",
        "pci",
        "motherboard",
        "platform",
        "management engine",
        "intel me",
        "amd psp",
        "serial io",
        "host controller",
        "storage controller",
        "pci bridge",
        "root port"
    ];

    public static DeviceClassification Classify(DiscoveredDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        return Classify(
            device.DeviceClass,
            device.Identity.InstanceId,
            device.HardwareIds.Select(x => x.Value),
            device.Manufacturer,
            device.DisplayName);
    }

    public static DeviceClassification Classify(
        string? deviceClass,
        string instanceId,
        IEnumerable<string>? hardwareIds,
        string? manufacturer,
        string? friendlyName)
    {
        var normalizedClass = Normalize(deviceClass);
        var normalizedInstanceId = Normalize(instanceId) ?? string.Empty;
        var hardware = (hardwareIds ?? []).Select(id => id?.Trim()).Where(id => !string.IsNullOrWhiteSpace(id)).Cast<string>().ToArray();
        var name = Normalize(friendlyName) ?? string.Empty;

        if (normalizedInstanceId.StartsWith("SOFTWARE\\", StringComparison.OrdinalIgnoreCase) ||
            IsIn(normalizedClass, SoftwareClasses))
        {
            return Build(DeviceCategory.VirtualOrSoftware, relevant: false, highPriority: false, lowValue: true, virtualOrSoftware: true, platformCritical: false);
        }

        if (IsClass(normalizedClass, "AudioEndpoint") &&
            LooksLikeUserAudioPeripheral(normalizedInstanceId, hardware, name))
        {
            var audioCategory = HasAnyKeyword(name, "microphone", "mic", "headset", "conference", "capture")
                ? DeviceCategory.Microphone
                : DeviceCategory.Audio;
            return Build(audioCategory, relevant: true, highPriority: false, lowValue: false, virtualOrSoftware: false, platformCritical: false);
        }

        if (IsIn(normalizedClass, LowValueClasses))
        {
            return Build(DeviceCategory.LowValueTechnical, relevant: false, highPriority: false, lowValue: true, virtualOrSoftware: false, platformCritical: false);
        }

        var category = ResolveCategory(normalizedClass, normalizedInstanceId, hardware, name);

        return category switch
        {
            DeviceCategory.VirtualOrSoftware => Build(category, relevant: false, highPriority: false, lowValue: true, virtualOrSoftware: true, platformCritical: false),
            DeviceCategory.LowValueTechnical => Build(category, relevant: false, highPriority: false, lowValue: true, virtualOrSoftware: false, platformCritical: false),
            DeviceCategory.Chipset or DeviceCategory.MotherboardPlatform or DeviceCategory.StorageController or DeviceCategory.UsbController
                => Build(category, relevant: true, highPriority: true, lowValue: false, virtualOrSoftware: false, platformCritical: true),
            DeviceCategory.Gpu or DeviceCategory.NetworkEthernet or DeviceCategory.NetworkWifi or DeviceCategory.Bluetooth
                => Build(category, relevant: true, highPriority: true, lowValue: false, virtualOrSoftware: false, platformCritical: false),
            DeviceCategory.Audio
                => Build(category, relevant: true, highPriority: false, lowValue: false, virtualOrSoftware: false, platformCritical: false),
            DeviceCategory.Keyboard or DeviceCategory.Mouse or DeviceCategory.Monitor or DeviceCategory.Printer or DeviceCategory.Scanner or DeviceCategory.Microphone or
            DeviceCategory.Camera or DeviceCategory.HidPeripheral or DeviceCategory.DockingStation or DeviceCategory.Touchpad or DeviceCategory.CardReader or DeviceCategory.Biometric
                => Build(category, relevant: true, highPriority: false, lowValue: false, virtualOrSoftware: false, platformCritical: false),
            DeviceCategory.Unknown => Build(category, relevant: false, highPriority: false, lowValue: false, virtualOrSoftware: false, platformCritical: false),
            _ => Build(category, relevant: true, highPriority: false, lowValue: false, virtualOrSoftware: false, platformCritical: false)
        };
    }

    private static DeviceCategory ResolveCategory(
        string? deviceClass,
        string instanceId,
        IReadOnlyCollection<string> hardwareIds,
        string friendlyName)
    {
        if (LooksLikeVirtualOrSoftware(instanceId, friendlyName))
        {
            return DeviceCategory.VirtualOrSoftware;
        }

        if (IsStorageController(deviceClass, instanceId, hardwareIds, friendlyName))
        {
            return DeviceCategory.StorageController;
        }

        if (IsUsbController(deviceClass, instanceId, hardwareIds, friendlyName))
        {
            return DeviceCategory.UsbController;
        }

        if (IsGpu(deviceClass, friendlyName))
        {
            return DeviceCategory.Gpu;
        }

        if (IsNetwork(deviceClass, instanceId, hardwareIds, friendlyName, out var networkCategory))
        {
            return networkCategory;
        }

        if (IsAudio(deviceClass, instanceId, friendlyName))
        {
            return DeviceCategory.Audio;
        }

        if (IsPlatformCritical(deviceClass, instanceId, hardwareIds, friendlyName, out var platformCategory))
        {
            return platformCategory;
        }

        if (IsPeripheral(deviceClass, instanceId, friendlyName, out var peripheralCategory))
        {
            return peripheralCategory;
        }

        if (LooksLowValueTechnical(deviceClass, instanceId, friendlyName))
        {
            return DeviceCategory.LowValueTechnical;
        }

        return DeviceCategory.Unknown;
    }

    private static bool IsStorageController(string? deviceClass, string instanceId, IReadOnlyCollection<string> hardwareIds, string friendlyName)
    {
        if (IsClass(deviceClass, "HDC") || IsClass(deviceClass, "SCSIAdapter") || IsClass(deviceClass, "SCSIController"))
        {
            return true;
        }

        return HasAnyKeyword(instanceId, friendlyName, hardwareIds, "nvme", "sata", "raid", "ahci", "storage controller", "storport");
    }

    private static bool IsUsbController(string? deviceClass, string instanceId, IReadOnlyCollection<string> hardwareIds, string friendlyName)
    {
        if (IsClass(deviceClass, "USB"))
        {
            return true;
        }

        return HasAnyKeyword(instanceId, friendlyName, hardwareIds, "usb", "xhci", "ehci", "uhci", "host controller");
    }

    private static bool IsGpu(string? deviceClass, string friendlyName)
    {
        return IsClass(deviceClass, "Display") ||
               HasAnyKeyword(friendlyName, "graphics", "display adapter", "radeon", "geforce", "arc");
    }

    private static bool IsNetwork(
        string? deviceClass,
        string instanceId,
        IReadOnlyCollection<string> hardwareIds,
        string friendlyName,
        out DeviceCategory category)
    {
        if (IsClass(deviceClass, "Bluetooth") || HasAnyKeyword(instanceId, friendlyName, hardwareIds, "bluetooth", "bth"))
        {
            category = DeviceCategory.Bluetooth;
            return true;
        }

        if (IsClass(deviceClass, "Net") || HasAnyKeyword(instanceId, friendlyName, hardwareIds, "ethernet", "lan", "gbe", "2.5gbe", "10gbe", "wireless", "wi-fi", "wlan", "802.11"))
        {
            category = HasAnyKeyword(instanceId, friendlyName, hardwareIds, "wireless", "wi-fi", "wlan", "802.11")
                ? DeviceCategory.NetworkWifi
                : DeviceCategory.NetworkEthernet;
            return true;
        }

        category = DeviceCategory.Unknown;
        return false;
    }

    private static bool IsAudio(string? deviceClass, string instanceId, string friendlyName)
    {
        if (IsClass(deviceClass, "AudioEndpoint"))
        {
            return false;
        }

        return IsClass(deviceClass, "Media") ||
               HasAnyKeyword(instanceId, friendlyName, "audio", "realtek", "sound", "hdaudio");
    }

    private static bool IsPlatformCritical(
        string? deviceClass,
        string instanceId,
        IReadOnlyCollection<string> hardwareIds,
        string friendlyName,
        out DeviceCategory category)
    {
        if (IsClass(deviceClass, "Chipset"))
        {
            category = DeviceCategory.Chipset;
            return true;
        }

        if (IsClass(deviceClass, "Motherboard") && HasAnyKeyword(instanceId, friendlyName, hardwareIds, PlatformKeywords))
        {
            category = DeviceCategory.MotherboardPlatform;
            return true;
        }

        if (!IsClass(deviceClass, "System") &&
            !HasAnyKeyword(instanceId, friendlyName, hardwareIds, PlatformKeywords))
        {
            category = DeviceCategory.Unknown;
            return false;
        }

        if (HasAnyKeyword(instanceId, friendlyName, hardwareIds, "chipset"))
        {
            category = DeviceCategory.Chipset;
            return true;
        }

        if (HasAnyKeyword(instanceId, friendlyName, hardwareIds, "smbus", "acpi", "pci", "management engine", "intel me", "amd psp", "platform"))
        {
            category = DeviceCategory.MotherboardPlatform;
            return true;
        }

        category = DeviceCategory.Unknown;
        return false;
    }

    private static bool IsPeripheral(string? deviceClass, string instanceId, string friendlyName, out DeviceCategory category)
    {
        if (IsClass(deviceClass, "Keyboard") || HasAnyKeyword(friendlyName, "keyboard"))
        {
            category = DeviceCategory.Keyboard;
            return true;
        }

        if (IsClass(deviceClass, "Mouse") || HasAnyKeyword(friendlyName, "mouse"))
        {
            category = DeviceCategory.Mouse;
            return true;
        }

        if (IsClass(deviceClass, "Monitor"))
        {
            category = DeviceCategory.Monitor;
            return true;
        }

        if (IsClass(deviceClass, "Printer") || HasAnyKeyword(friendlyName, "printer"))
        {
            category = DeviceCategory.Printer;
            return true;
        }

        if (IsClass(deviceClass, "Image") || IsClass(deviceClass, "Camera") || HasAnyKeyword(friendlyName, "camera", "webcam"))
        {
            category = DeviceCategory.Camera;
            return true;
        }

        if (HasAnyKeyword(friendlyName, "microphone", "mic array"))
        {
            category = DeviceCategory.Microphone;
            return true;
        }

        if (IsClass(deviceClass, "HIDClass"))
        {
            if (HasAnyKeyword(friendlyName, "touchpad"))
            {
                category = DeviceCategory.Touchpad;
                return true;
            }

            if (HasAnyKeyword(friendlyName, "fingerprint", "biometric"))
            {
                category = DeviceCategory.Biometric;
                return true;
            }

            category = DeviceCategory.HidPeripheral;
            return true;
        }

        if (HasAnyKeyword(friendlyName, "scanner"))
        {
            category = DeviceCategory.Scanner;
            return true;
        }

        if (HasAnyKeyword(friendlyName, "dock", "thunderbolt"))
        {
            category = DeviceCategory.DockingStation;
            return true;
        }

        if (HasAnyKeyword(friendlyName, "card reader", "sd host"))
        {
            category = DeviceCategory.CardReader;
            return true;
        }

        category = DeviceCategory.Unknown;
        return false;
    }

    private static bool LooksLikeVirtualOrSoftware(string instanceId, string friendlyName)
    {
        if (instanceId.StartsWith("SWD\\", StringComparison.OrdinalIgnoreCase) &&
            !instanceId.StartsWith("SWD\\BTH", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (instanceId.StartsWith("ROOT\\", StringComparison.OrdinalIgnoreCase) &&
            HasAnyKeyword(friendlyName, RootLowValueNameKeywords))
        {
            return true;
        }

        return HasAnyKeyword(friendlyName, "virtual", "software device", "enumerator") &&
               !HasAnyKeyword(friendlyName, "camera", "microphone", "keyboard", "mouse");
    }

    private static bool LooksLowValueTechnical(string? deviceClass, string instanceId, string friendlyName)
    {
        if (IsIn(deviceClass, LowValueClasses))
        {
            return true;
        }

        if (HasPrefix(instanceId, LowValueInstancePrefixes))
        {
            return true;
        }

        if (instanceId.StartsWith("ROOT\\", StringComparison.OrdinalIgnoreCase) &&
            HasAnyKeyword(friendlyName, RootLowValueNameKeywords))
        {
            return true;
        }

        return false;
    }

    private static bool LooksLikeUserAudioPeripheral(string instanceId, IReadOnlyCollection<string> hardwareIds, string friendlyName)
    {
        if (HasAnyKeyword(friendlyName, "microphone", "mic", "headset", "conference", "speakerphone", "usb audio", "webcam"))
        {
            return true;
        }

        if (HasAnyKeyword(instanceId, "usb", "hdaudio"))
        {
            return true;
        }

        return HasAnyKeyword(hardwareIds, "usb\\", "hdaudio\\", "bluetooth\\", "bth\\");
    }

    private static DeviceClassification Build(
        DeviceCategory category,
        bool relevant,
        bool highPriority,
        bool lowValue,
        bool virtualOrSoftware,
        bool platformCritical)
    {
        return new DeviceClassification(
            category,
            IsRelevantForUser: relevant,
            IsHighPriority: highPriority,
            IsLowValueTechnical: lowValue,
            IsVirtualOrSoftware: virtualOrSoftware,
            IsPlatformCritical: platformCritical,
            IsRecommendationScopeCandidate: relevant || platformCritical);
    }

    private static bool IsClass(string? value, string expected)
        => string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsIn(string? value, IReadOnlyCollection<string> values)
        => !string.IsNullOrWhiteSpace(value) && values.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

    private static bool HasPrefix(string value, IEnumerable<string> prefixes)
        => prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool HasAnyKeyword(IEnumerable<string> values, params string[] keywords)
        => values.Any(value => HasAnyKeyword(value, keywords));

    private static bool HasAnyKeyword(string value, params string[] keywords)
        => keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    private static bool HasAnyKeyword(string value1, string value2, params string[] keywords)
        => HasAnyKeyword(value1, keywords) || HasAnyKeyword(value2, keywords);

    private static bool HasAnyKeyword(string value1, string value2, IReadOnlyCollection<string> values3, params string[] keywords)
        => HasAnyKeyword(value1, keywords) || HasAnyKeyword(value2, keywords) || HasAnyKeyword(values3, keywords);
}
