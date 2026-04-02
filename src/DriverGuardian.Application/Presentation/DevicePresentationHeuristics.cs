using DriverGuardian.Contracts.DeviceDiscovery;

namespace DriverGuardian.Application.Presentation;

public static class DevicePresentationHeuristics
{
    private static readonly string[] HighPriorityClasses =
    [
        "Display",
        "Net",
        "Media",
        "Bluetooth",
        "USB",
        "HDC",
        "SCSIAdapter",
        "Camera",
        "Image",
        "Keyboard",
        "Mouse",
        "HIDClass"
    ];

    private static readonly string[] LowValueClasses =
    [
        "SoftwareDevice",
        "PrintQueue",
        "LegacyDriver",
        "AudioEndpoint",
        "SoftwareComponent"
    ];

    private static readonly string[] LowValueInstancePrefixes =
    [
        "SWD\\",
        "ROOT\\",
        "PRINTENUM\\",
        "MMDEVAPI\\"
    ];

    public static bool IsUserRelevant(DiscoveredDevice? device, bool hasRecommendation)
    {
        if (hasRecommendation)
        {
            return true;
        }

        if (device is null)
        {
            return true;
        }

        var instanceId = device.Identity.InstanceId;
        var isLowValuePrefix = HasPrefix(instanceId, LowValueInstancePrefixes);
        var isLowValueClass = IsInClassList(device.DeviceClass, LowValueClasses);
        return !(isLowValuePrefix || isLowValueClass);
    }

    public static int ResolvePriorityBucket(DiscoveredDevice? device, bool hasRecommendation)
    {
        if (hasRecommendation)
        {
            return 0;
        }

        if (device is null)
        {
            return 3;
        }

        if (IsInClassList(device.DeviceClass, HighPriorityClasses))
        {
            return 1;
        }

        if (IsUserRelevant(device, hasRecommendation))
        {
            return 2;
        }

        return 4;
    }

    public static string BuildUserFacingName(DiscoveredDevice? device, string fallbackInstanceId)
    {
        var candidate = device?.DisplayName;
        if (!string.IsNullOrWhiteSpace(candidate) && !LooksTechnical(candidate))
        {
            return candidate.Trim();
        }

        if (!string.IsNullOrWhiteSpace(device?.Manufacturer) && !string.IsNullOrWhiteSpace(device?.DeviceClass))
        {
            return $"{device.Manufacturer!.Trim()} ({device.DeviceClass!.Trim()})";
        }

        if (!string.IsNullOrWhiteSpace(device?.DeviceClass))
        {
            return $"Устройство ({device.DeviceClass!.Trim()})";
        }

        return fallbackInstanceId;
    }

    private static bool IsInClassList(string? value, IReadOnlyCollection<string> classList)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return classList.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasPrefix(string value, IReadOnlyCollection<string> prefixes)
    {
        return prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksTechnical(string value)
    {
        var trimmed = value.Trim();
        return HasPrefix(trimmed, LowValueInstancePrefixes) ||
               trimmed.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("VID_", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("VEN_", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains('{', StringComparison.OrdinalIgnoreCase);
    }
}
