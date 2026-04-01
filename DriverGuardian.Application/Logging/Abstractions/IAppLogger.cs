using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Application.Logging.Abstractions;

public interface IAppLogger
{
    Task LogAsync(LogMessage message, CancellationToken cancellationToken);
}
