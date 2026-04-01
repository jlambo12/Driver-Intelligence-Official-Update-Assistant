using System.Threading;
using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Infrastructure.Logging.Context;

public sealed class AsyncLocalOperationContextAccessor : IOperationContextAccessor
{
    private static readonly AsyncLocal<OperationContext?> CurrentHolder = new();

    public OperationContext? Current
    {
        get => CurrentHolder.Value;
        set => CurrentHolder.Value = value;
    }
}
