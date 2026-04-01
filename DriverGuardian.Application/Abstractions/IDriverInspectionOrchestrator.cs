using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Application.Abstractions;

public interface IDriverInspectionOrchestrator
{
    Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(CancellationToken cancellationToken);
}
