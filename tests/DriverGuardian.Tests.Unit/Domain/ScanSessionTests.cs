using DriverGuardian.Domain.Scanning;

namespace DriverGuardian.Tests.Unit.Domain;

public sealed class ScanSessionTests
{
    [Fact]
    public void Complete_ShouldThrow_WhenTimestampIsBeforeStart()
    {
        var started = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var session = ScanSession.Start(Guid.NewGuid(), started);

        Assert.Throws<ArgumentException>(() => session.Complete(started.AddMinutes(-1)));
    }
}
