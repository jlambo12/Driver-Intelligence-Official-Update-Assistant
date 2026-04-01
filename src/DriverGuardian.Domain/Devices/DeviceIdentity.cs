namespace DriverGuardian.Domain.Devices;

public sealed record DeviceIdentity
{
    public DeviceIdentity(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Device instance id is required.", nameof(instanceId));
        }

        InstanceId = instanceId.Trim();
    }

    public string InstanceId { get; }
}
