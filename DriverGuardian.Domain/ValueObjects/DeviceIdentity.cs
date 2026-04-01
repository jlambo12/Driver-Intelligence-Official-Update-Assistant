using DriverGuardian.Domain.Common;

namespace DriverGuardian.Domain.ValueObjects;

public sealed record DeviceIdentity
{
    public string InstanceId { get; }
    public string DisplayName { get; }

    public DeviceIdentity(string instanceId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new DomainException("Device instance id is required.");

        InstanceId = instanceId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Unknown Device" : displayName.Trim();
    }
}
