namespace DriverGuardian.Application.Logging.Models;

public readonly record struct CorrelationId(string Value)
{
    public static CorrelationId Create() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}
