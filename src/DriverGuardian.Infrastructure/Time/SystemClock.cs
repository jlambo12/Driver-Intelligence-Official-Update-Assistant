using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
