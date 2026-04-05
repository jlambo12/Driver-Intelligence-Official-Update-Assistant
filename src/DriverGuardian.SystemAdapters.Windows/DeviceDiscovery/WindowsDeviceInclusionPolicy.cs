namespace DriverGuardian.SystemAdapters.Windows.DeviceDiscovery;

using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Domain.Settings;

public static class WindowsDeviceInclusionPolicy
{
    private static readonly HashSet<string> ExcludedClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "SoftwareComponent",
        "SoftwareDevice",
        "LegacyDriver"
    };

    public static bool ShouldInclude(WindowsPnpEntitySnapshot snapshot, DeviceScanProfile profile = DeviceScanProfile.Balanced)
    {
        if (string.IsNullOrWhiteSpace(snapshot.InstanceId))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(snapshot.DeviceClass) && ExcludedClasses.Contains(snapshot.DeviceClass))
        {
            return false;
        }

        var classification = DeviceRelevanceClassifier.Classify(
            snapshot.DeviceClass,
            snapshot.InstanceId,
            snapshot.HardwareIds,
            snapshot.Manufacturer,
            snapshot.FriendlyName);

        if (classification.IsVirtualOrSoftware)
        {
            return false;
        }

        if (profile == DeviceScanProfile.Comprehensive)
        {
            return true;
        }

        return profile switch
        {
            DeviceScanProfile.Minimal => ShouldIncludeInMinimalProfile(snapshot, classification),
            _ => ShouldIncludeInBalancedProfile(snapshot, classification)
        };
    }

    private static bool ShouldIncludeInMinimalProfile(WindowsPnpEntitySnapshot snapshot, DeviceClassification classification)
    {
        if (classification.IsHighPriority || classification.IsPlatformCritical)
        {
            return true;
        }

        if (classification.Category is DeviceCategory.Keyboard or DeviceCategory.Mouse)
        {
            return true;
        }

        if (string.Equals(snapshot.DeviceClass, "AudioEndpoint", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return false;
    }

    private static bool ShouldIncludeInBalancedProfile(WindowsPnpEntitySnapshot snapshot, DeviceClassification classification)
    {
        if (classification.IsRelevantForUser || classification.IsPlatformCritical)
        {
            return true;
        }

        if (string.Equals(snapshot.DeviceClass, "System", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
