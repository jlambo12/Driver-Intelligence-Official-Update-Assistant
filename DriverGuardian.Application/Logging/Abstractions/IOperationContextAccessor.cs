using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Application.Logging.Abstractions;

public interface IOperationContextAccessor
{
    OperationContext? Current { get; set; }
}
