using DriverGuardian.Domain.Entities;
using DriverGuardian.Infrastructure.Abstractions;

namespace DriverGuardian.Infrastructure.Settings;

public sealed class InMemorySettingsRepository : ISettingsRepository
{
    private AppSettings _settings = AppSettings.Default;

    public Task<AppSettings> GetAsync(CancellationToken cancellationToken) => Task.FromResult(_settings);

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        _settings = settings;
        return Task.CompletedTask;
    }
}
