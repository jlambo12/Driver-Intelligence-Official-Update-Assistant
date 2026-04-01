using System.Management;
using DriverGuardian.Contracts.DeviceDiscovery;

namespace DriverGuardian.SystemAdapters.Windows.DeviceDiscovery;

internal static class WindowsDeviceDiscoveryMapper
{
    public static DiscoveredDevice Map(WindowsPnpEntitySnapshot snapshot)
    {
        var presenceStatus = snapshot.ConfigManagerErrorCode switch
        {
            0 => DevicePresenceStatus.Present,
            int code when code > 0 => DevicePresenceStatus.Problem,
            _ => DevicePresenceStatus.Unknown
        };

        var rawStatus = string.IsNullOrWhiteSpace(snapshot.Status)
            ? presenceStatus.ToString()
            : snapshot.Status;

        return DiscoveredDevice.Create(
            instanceId: snapshot.InstanceId!,
            displayName: snapshot.FriendlyName,
            hardwareIds: snapshot.HardwareIds,
            manufacturer: snapshot.Manufacturer,
            deviceClass: snapshot.DeviceClass,
            presenceStatus: presenceStatus,
            rawStatus: rawStatus);
    }
}

internal sealed record WindowsPnpEntitySnapshot(
    string? InstanceId,
    string? FriendlyName,
    IReadOnlyCollection<string> HardwareIds,
    string? Manufacturer,
    string? DeviceClass,
    int? ConfigManagerErrorCode,
    string? Status)
{
    public static WindowsPnpEntitySnapshot From(ManagementObject entity)
    {
        var hardwareIds = entity["HardwareID"] switch
        {
            string single => [single],
            string[] array => array,
            _ => Array.Empty<string>()
        };

        var instanceId = (entity["PNPDeviceID"] as string) ?? (entity["DeviceID"] as string);

        return new WindowsPnpEntitySnapshot(
            InstanceId: instanceId,
            FriendlyName: entity["Name"] as string,
            HardwareIds: hardwareIds,
            Manufacturer: entity["Manufacturer"] as string,
            DeviceClass: entity["PNPClass"] as string,
            ConfigManagerErrorCode: entity["ConfigManagerErrorCode"] as int?,
            Status: entity["Status"] as string);
    }
}
