using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Infrastructure.Abstractions;

public interface ISettingsRepository
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
