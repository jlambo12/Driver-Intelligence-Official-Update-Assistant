namespace DriverGuardian.Domain.Devices;

public sealed record HardwareIdentifier
{
    public HardwareIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Hardware identifier is required.", nameof(value));
        }

        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }
}
