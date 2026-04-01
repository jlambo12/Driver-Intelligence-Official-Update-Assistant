using DriverGuardian.Domain.Settings;
using DriverGuardian.Infrastructure.Settings;

namespace DriverGuardian.Tests.Unit.Infrastructure.Settings;

public sealed class JsonFileSettingsRepositoryTests
{
    [Fact]
    public async Task GetAsync_WhenFileMissing_ShouldReturnDefault()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"driver-guardian-settings-{Guid.NewGuid():N}.json");
        var repository = new JsonFileSettingsRepository(filePath);

        var settings = await repository.GetAsync(CancellationToken.None);

        Assert.Equal(AppSettings.Default, settings);
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ShouldRoundTripNormalizedSettings()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"driver-guardian-settings-{Guid.NewGuid():N}.json");
        var repository = new JsonFileSettingsRepository(filePath);

        try
        {
            var update = AppSettings.Default with
            {
                Localization = new LocalizationPreferences("  fr-FR  "),
                History = new HistoryPreferences(6, HistoryRetentionStrategy.KeepMostRecent),
                Reports = AppSettings.Default.Reports with { DefaultFormat = ShareableReportFormat.PlainText },
                WorkflowGuidance = AppSettings.Default.WorkflowGuidance with { ShowPostInstallVerificationHints = false }
            };

            await repository.SaveAsync(update, CancellationToken.None);
            var reloaded = await repository.GetAsync(CancellationToken.None);

            Assert.Equal("fr-FR", reloaded.UiCulture);
            Assert.Equal(10, reloaded.History.MaxEntries);
            Assert.Equal(ShareableReportFormat.PlainText, reloaded.Reports.DefaultFormat);
            Assert.False(reloaded.WorkflowGuidance.ShowPostInstallVerificationHints);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
