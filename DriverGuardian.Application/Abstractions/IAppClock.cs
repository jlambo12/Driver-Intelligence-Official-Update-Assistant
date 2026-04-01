namespace DriverGuardian.Application.Abstractions;

public interface IAppClock
{
    DateTimeOffset UtcNow { get; }
}
