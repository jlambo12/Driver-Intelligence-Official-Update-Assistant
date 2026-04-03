using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Settings;
using DriverGuardian.Infrastructure.DiagnosticLogging;

namespace DriverGuardian.Tests.Unit.Infrastructure.DiagnosticLogging;

public sealed class SettingsDiagnosticLoggerTests
{
    [Fact]
    public async Task LogInfoAsync_ShouldNotWriteFile_WhenDiagnosticLoggingDisabled()
    {
        var logsDirectory = Path.Combine(Path.GetTempPath(), $"driverguardian-logs-{Guid.NewGuid():N}");
        var repository = new MutableSettingsRepository(AppSettings.Default with
        {
            DiagnosticLogging = new DiagnosticLoggingPreferences(false, string.Empty)
        });
        var logger = new SettingsDiagnosticLogger(repository, logsDirectory);

        await logger.LogInfoAsync("scan.test", "disabled", CancellationToken.None);

        Assert.False(Directory.Exists(logsDirectory));
    }

    [Fact]
    public async Task LogInfoAsync_ShouldUseCustomDirectory_WhenDiagnosticLoggingEnabledWithCustomPath()
    {
        var customDirectory = Path.Combine(Path.GetTempPath(), $"driverguardian-custom-logs-{Guid.NewGuid():N}");
        var repository = new MutableSettingsRepository(AppSettings.Default with
        {
            DiagnosticLogging = new DiagnosticLoggingPreferences(true, customDirectory)
        });
        var logger = new SettingsDiagnosticLogger(repository, Path.Combine(Path.GetTempPath(), "default-logs-unused"));

        try
        {
            await logger.LogInfoAsync("scan.test", "enabled", CancellationToken.None);

            var filePath = Directory.GetFiles(customDirectory, "scan-*.log").Single();
            var content = await File.ReadAllTextAsync(filePath, CancellationToken.None);

            Assert.Contains("enabled", content, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(customDirectory))
            {
                Directory.Delete(customDirectory, recursive: true);
            }
        }
    }

    private sealed class MutableSettingsRepository(AppSettings settings) : ISettingsRepository
    {
        private AppSettings _settings = settings;

        public Task<AppSettings> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult(_settings);

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
        {
            _settings = settings;
            return Task.CompletedTask;
        }
    }
}
