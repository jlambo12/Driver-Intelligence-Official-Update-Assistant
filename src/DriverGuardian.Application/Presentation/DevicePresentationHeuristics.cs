using DriverGuardian.Contracts.DeviceDiscovery;

namespace DriverGuardian.Application.Presentation;

public static class DevicePresentationHeuristics
{
    public static bool IsUserRelevant(DiscoveredDevice? device, bool hasRecommendation)
    {
        if (hasRecommendation || device is null)
        {
            return true;
        }

        return DeviceRelevanceClassifier.Classify(device).IsRelevantForUser;
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

        var classification = DeviceRelevanceClassifier.Classify(device);
        if (classification.IsHighPriority)
        {
            return 1;
        }

        if (classification.IsRelevantForUser)
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

    private static bool LooksTechnical(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith("SWD\\", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("ROOT\\", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("PRINTENUM\\", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("MMDEVAPI\\", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("VID_", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("VEN_", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains('{', StringComparison.OrdinalIgnoreCase);
    }
}
