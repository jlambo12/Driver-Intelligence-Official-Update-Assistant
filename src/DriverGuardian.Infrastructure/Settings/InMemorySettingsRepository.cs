using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Settings;

namespace DriverGuardian.Infrastructure.Settings;

public sealed class InMemorySettingsRepository : ISettingsRepository
{
    private AppSettings _settings = AppSettings.Default;

    public Task<AppSettings> GetAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_settings);
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        _settings = settings;
        return Task.CompletedTask;
    }
}
