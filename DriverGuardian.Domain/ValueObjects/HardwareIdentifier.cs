using DriverGuardian.Domain.Common;

namespace DriverGuardian.Domain.ValueObjects;

public sealed record HardwareIdentifier
{
    public string Value { get; }

    public HardwareIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Hardware identifier cannot be empty.");

        Value = value.Trim().ToUpperInvariant();
    }
}
