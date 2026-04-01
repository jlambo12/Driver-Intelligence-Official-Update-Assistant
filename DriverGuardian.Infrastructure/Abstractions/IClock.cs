namespace DriverGuardian.Infrastructure.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
