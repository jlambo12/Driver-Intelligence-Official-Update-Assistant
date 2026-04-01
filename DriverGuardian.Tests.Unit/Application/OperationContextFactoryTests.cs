using DriverGuardian.Application.Logging.Models;
using DriverGuardian.Infrastructure.Logging.Context;
using DriverGuardian.Infrastructure.Time;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class OperationContextFactoryTests
{
    [Fact]
    public void Create_Should_AssignStableOperationId()
    {
        var factory = new OperationContextFactory(new SystemClock());

        var context = factory.Create("op", "source", CorrelationId.Create());

        Assert.False(string.IsNullOrWhiteSpace(context.OperationId));
        Assert.Equal(context.OperationId, context.OperationId);
    }
}
