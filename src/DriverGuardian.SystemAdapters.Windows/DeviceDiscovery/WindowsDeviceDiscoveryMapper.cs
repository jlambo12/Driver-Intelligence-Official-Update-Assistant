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

public sealed record WindowsPnpEntitySnapshot(
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
        var instanceId = ReadString(entity, "PNPDeviceID") ?? ReadString(entity, "DeviceID");

        return new WindowsPnpEntitySnapshot(
            InstanceId: instanceId,
            FriendlyName: ReadString(entity, "Name"),
            HardwareIds: ReadHardwareIds(entity),
            Manufacturer: ReadString(entity, "Manufacturer"),
            DeviceClass: ReadString(entity, "PNPClass"),
            ConfigManagerErrorCode: ReadInt(entity, "ConfigManagerErrorCode"),
            Status: ReadString(entity, "Status"));
    }

    private static IReadOnlyCollection<string> ReadHardwareIds(ManagementObject entity)
    {
        var rawValue = entity["HardwareID"];

        return rawValue switch
        {
            string single => [single],
            string[] array => array,
            IEnumerable<string> enumerable => enumerable.ToArray(),
            _ => Array.Empty<string>()
        };
    }

    private static string? ReadString(ManagementObject entity, string propertyName)
    {
        return entity[propertyName] as string;
    }

    private static int? ReadInt(ManagementObject entity, string propertyName)
    {
        return entity[propertyName] switch
        {
            int value => value,
            uint value => checked((int)value),
            ushort value => value,
            short value => value,
            byte value => value,
            _ => null
        };
    }
}
