using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Settings;

namespace DriverGuardian.Infrastructure.Settings;

public sealed class InMemorySettingsRepository : ISettingsRepository
{
    private readonly object _gate = new();
    private AppSettings _settings = AppSettings.Default;

    public Task<AppSettings> GetAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_settings);
        }
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_gate)
        {
            _settings = settings.Normalize();
        }

        return Task.CompletedTask;
    }
}
