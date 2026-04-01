using DriverGuardian.Domain.Devices;

namespace DriverGuardian.Contracts.DeviceDiscovery;

public enum DevicePresenceStatus
{
    Unknown = 0,
    Present = 1,
    Problem = 2
}

public sealed record DiscoveredDevice
{
    public DiscoveredDevice(
        DeviceIdentity identity,
        string displayName,
        IReadOnlyCollection<HardwareIdentifier> hardwareIds,
        string? manufacturer,
        string? deviceClass,
        DevicePresenceStatus presenceStatus,
        string? rawStatus)
    {
        Identity = identity;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? identity.InstanceId : displayName.Trim();
        HardwareIds = hardwareIds;
        Manufacturer = NormalizeOptional(manufacturer);
        DeviceClass = NormalizeOptional(deviceClass);
        PresenceStatus = presenceStatus;
        RawStatus = NormalizeOptional(rawStatus);
    }

    public DeviceIdentity Identity { get; }
    public string DisplayName { get; }
    public IReadOnlyCollection<HardwareIdentifier> HardwareIds { get; }
    public string? Manufacturer { get; }
    public string? DeviceClass { get; }
    public DevicePresenceStatus PresenceStatus { get; }
    public string? RawStatus { get; }

    public static DiscoveredDevice Create(
        string instanceId,
        string? displayName,
        IEnumerable<string>? hardwareIds,
        string? manufacturer,
        string? deviceClass,
        DevicePresenceStatus presenceStatus,
        string? rawStatus)
    {
        var normalizedHardwareIds = (hardwareIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => new HardwareIdentifier(id))
            .Distinct()
            .ToArray();

        var identity = new DeviceIdentity(instanceId);
        var name = string.IsNullOrWhiteSpace(displayName) ? identity.InstanceId : displayName.Trim();

        return new DiscoveredDevice(identity, name, normalizedHardwareIds, manufacturer, deviceClass, presenceStatus, rawStatus);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
