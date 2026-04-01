using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Application.Logging.Abstractions;

public interface ILogDiagnosticsQuery
{
    IReadOnlyCollection<LogEntry> GetRecent(int take = 200);
}
