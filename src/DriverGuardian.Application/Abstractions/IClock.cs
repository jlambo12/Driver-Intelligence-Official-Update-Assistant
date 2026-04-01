namespace DriverGuardian.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
