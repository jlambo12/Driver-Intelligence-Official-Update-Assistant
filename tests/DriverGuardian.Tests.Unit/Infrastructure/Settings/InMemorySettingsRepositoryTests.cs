using DriverGuardian.Domain.Settings;
using DriverGuardian.Infrastructure.Settings;

namespace DriverGuardian.Tests.Unit.Infrastructure.Settings;

public sealed class InMemorySettingsRepositoryTests
{
    [Fact]
    public async Task SaveAsync_ShouldStoreNormalizedSettings()
    {
        var repository = new InMemorySettingsRepository();
        var updated = AppSettings.Default with
        {
            Localization = new LocalizationPreferences("  en-US "),
            History = new HistoryPreferences(9999, HistoryRetentionStrategy.KeepMostRecent)
        };

        await repository.SaveAsync(updated, CancellationToken.None);
        var stored = await repository.GetAsync(CancellationToken.None);

        Assert.Equal("en-US", stored.UiCulture);
        Assert.Equal(500, stored.History.MaxEntries);
    }
}
