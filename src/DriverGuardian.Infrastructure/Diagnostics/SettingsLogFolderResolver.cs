using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Infrastructure.Diagnostics;

public sealed class SettingsLogFolderResolver(
    ISettingsRepository settingsRepository,
    string defaultLogFolderPath) : ILogFolderResolver
{
    public async Task<string> GetEffectiveLogFolderAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        var configuredPath = settings.DiagnosticLogging.CustomFolderPath;
        var effective = string.IsNullOrWhiteSpace(configuredPath)
            ? defaultLogFolderPath
            : configuredPath;

        return Path.GetFullPath(effective);
    }
}
