using DriverGuardian.Domain.Settings;
using DriverGuardian.Infrastructure.Settings;
using DriverGuardian.UI.Wpf.Commands;
using DriverGuardian.UI.Wpf.ViewModels.Sections;

namespace DriverGuardian.Tests.Unit.Application.Settings;

public sealed class SettingsSectionViewModelTests
{
    [Fact]
    public async Task SaveSettingsCommand_ShouldPersistSelectedScanProfile()
    {
        var repository = new InMemorySettingsRepository();
        await repository.SaveAsync(AppSettings.Default, CancellationToken.None);

        var first = new SettingsSectionViewModel(repository, "C:/logs/default");
        await first.LoadSettingsAsync("C:/logs/default", CancellationToken.None);
        first.SelectedScanProfile = first.AvailableScanProfiles.First(p => p.Value == DeviceScanProfile.Minimal);

        var saveCommand = Assert.IsType<AsyncRelayCommand>(first.SaveSettingsCommand);
        await saveCommand.ExecuteAsync();

        var second = new SettingsSectionViewModel(repository, "C:/logs/default");
        await second.LoadSettingsAsync("C:/logs/default", CancellationToken.None);

        Assert.Equal(DeviceScanProfile.Minimal, second.SelectedScanProfile.Value);
    }

    [Fact]
    public async Task LoadSettingsAsync_ShouldHonorCancellationToken()
    {
        var repository = new InMemorySettingsRepository();
        var viewModel = new SettingsSectionViewModel(repository, "C:/logs/default");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => viewModel.LoadSettingsAsync("C:/logs/default", cts.Token));
    }
}
