using DriverGuardian.Application.Abstractions;
using DriverGuardian.Infrastructure.Abstractions;

namespace DriverGuardian.Infrastructure.Time;

public sealed class AppClockAdapter(IClock clock) : IAppClock
{
    public DateTimeOffset UtcNow => clock.UtcNow;
}
