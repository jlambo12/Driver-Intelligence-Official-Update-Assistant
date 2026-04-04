namespace DriverGuardian.SystemAdapters.Windows.DeviceDiscovery;
using DriverGuardian.Domain.Settings;

public static class WindowsDeviceInclusionPolicy
{
    private static readonly HashSet<string> MinimalIncludedClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Display",
        "System",
        "Net",
        "Keyboard",
        "Mouse",
        "HIDClass",
        "Media",
        "AudioEndpoint"
    };

    private static readonly HashSet<string> BalancedIncludedClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Display",
        "System",
        "Net",
        "HIDClass",
        "Keyboard",
        "Mouse",
        "Media",
        "AudioEndpoint",
        "Bluetooth",
        "Image",
        "Camera",
        "USB",
        "Ports",
        "Processor",
        "Biometric"
    };

    private static readonly HashSet<string> ExcludedClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "SoftwareComponent",
        "SoftwareDevice"
    };

    private static readonly string[] IncludedHardwarePrefixes =
    [
        "PCI\\",
        "USB\\",
        "HID\\",
        "ACPI\\",
        "BTH\\",
        "HDAUDIO\\",
        "SCSI\\",
        "ROOT\\"
    ];

    public static bool ShouldInclude(WindowsPnpEntitySnapshot snapshot, DeviceScanProfile profile = DeviceScanProfile.Balanced)
    {
        if (string.IsNullOrWhiteSpace(snapshot.InstanceId))
        {
            return false;
        }

        if (snapshot.InstanceId.StartsWith("SOFTWARE\\", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (snapshot.InstanceId.StartsWith("SWD\\", StringComparison.OrdinalIgnoreCase)
            && !snapshot.InstanceId.StartsWith("SWD\\MMDEVAPI", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (snapshot.DeviceClass is not null)
        {
            var deviceClass = snapshot.DeviceClass.Trim();
            if (deviceClass.Length == 0)
            {
                return false;
            }

            if (ExcludedClasses.Contains(deviceClass))
            {
                return false;
            }

            if (profile == DeviceScanProfile.Comprehensive)
            {
                return true;
            }

            var includedClasses = GetIncludedClasses(profile);
            if (!includedClasses.Contains(deviceClass))
            {
                return false;
            }

            return true;
        }

        return snapshot.HardwareIds.Any(id => IncludedHardwarePrefixes.Any(prefix => id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
    }

    private static HashSet<string> GetIncludedClasses(DeviceScanProfile profile) =>
        profile == DeviceScanProfile.Minimal
            ? MinimalIncludedClasses
            : BalancedIncludedClasses;
}
