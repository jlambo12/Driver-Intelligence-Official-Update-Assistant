using DriverGuardian.Domain.Settings;

namespace DriverGuardian.Application.Abstractions;

public interface ISettingsRepository
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
