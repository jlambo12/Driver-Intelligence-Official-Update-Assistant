using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Domain.Settings;
using DriverGuardian.Infrastructure.DiagnosticLogging;
using DriverGuardian.Infrastructure.Settings;
using DriverGuardian.UI.Wpf.ViewModels;
using DriverGuardian.UI.Wpf.Services;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class MainViewModelCoordinationTests
{
    [Fact]
    public async Task InitializeAsync_LoadsSettingsIntoSectionState()
    {
        var settings = AppSettings.Default with
        {
            History = AppSettings.Default.History with { MaxEntries = 133 },
            Reports = AppSettings.Default.Reports with { DefaultFormat = ShareableReportFormat.PlainText },
            WorkflowGuidance = AppSettings.Default.WorkflowGuidance with { ShowPostInstallVerificationHints = false }
        };

        var repository = new InMemorySettingsRepository(settings);
        var viewModel = CreateMainViewModel(new StubMainScreenWorkflow(CreateResult()), repository);

        await viewModel.InitializeAsync();

        Assert.Equal(133, viewModel.SettingsSection.HistoryMaxEntries);
        Assert.False(viewModel.SettingsSection.ShowVerificationHints);
        Assert.Equal(ShareableReportFormat.PlainText, viewModel.SettingsSection.SelectedReportFormat.Value);
    }

    [Fact]
    public async Task PreviewFirstRunScenario_ResetsHistoryAndReportState()
    {
        var previewWorkflow = new PreviewScenarioMainScreenWorkflow();
        var viewModel = CreateMainViewModel(previewWorkflow, new InMemorySettingsRepository(AppSettings.Default));

        await viewModel.InitializeAsync();

        Assert.Empty(viewModel.HistorySection.RecentHistory);
        Assert.Equal("Сохранение отчёта недоступно до первого завершённого анализа.", viewModel.ReportSection.ReportExportStatusText);
        Assert.False(viewModel.WorkflowSection.ShowSecondaryRecommendations);
    }

    [Fact]
    public async Task ScanAsync_PropagatesWorkflowResultToSections()
    {
        var result = CreateResult(
            reportPayload: new ReportExportPayload("export-name", "plain", "markdown"),
            recentHistory:
            [
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow, RecentHistoryEntryKind.Scan, Guid.NewGuid(), 1, 1, 0, null, null)
            ]);

        var viewModel = CreateMainViewModel(new StubMainScreenWorkflow(result), new InMemorySettingsRepository(AppSettings.Default));
        await viewModel.InitializeAsync();

        await ExecuteAsync(viewModel.ScanCommand);

        Assert.Equal("Подготовлено: выберите папку и сохраните отчёт вручную.", viewModel.ReportSection.ReportExportStatusText);
        Assert.Single(viewModel.HistorySection.RecentHistory);
        Assert.True(viewModel.State.Results.HasScanData);
    }

    private static MainViewModel CreateMainViewModel(IMainScreenWorkflow workflow, ISettingsRepository settingsRepository)
        => new(
            workflow,
            settingsRepository,
            new FakeReportFileSaveService(),
            new FakeDiagnosticLogsFolderService(),
            new FakeOfficialSourceLauncher());

    private static async Task ExecuteAsync(System.Windows.Input.ICommand command)
    {
        var task = ((DriverGuardian.UI.Wpf.Commands.AsyncRelayCommand)command).ExecuteAsync(null);
        await task;
    }

    private static MainScreenWorkflowResult CreateResult(
        ReportExportPayload? reportPayload = null,
        IReadOnlyCollection<RecentHistoryEntryResult>? recentHistory = null)
        => new(
            ScanExecutionStatus.Completed,
            [],
            2,
            2,
            1,
            1,
            1,
            1,
            0,
            "verification",
            "ru-RU",
            Guid.NewGuid(),
            reportPayload ?? new ReportExportPayload("name", "", ""),
            [
                new RecommendationDetailResult(
                    "device",
                    "id",
                    0,
                    true,
                    "reason",
                    "1.0",
                    "provider",
                    "1.1",
                    true,
                    true,
                    true,
                    "hint")
            ],
            new OpenOfficialSourceActionResult(true, OfficialSourceResolutionOutcome.ConfirmedVendorSupportPage, OfficialSourceActionTarget.SourcePage, "status", "https://example.com", null),
            recentHistory ?? []);

    private sealed class StubMainScreenWorkflow(MainScreenWorkflowResult result) : IMainScreenWorkflow
    {
        public Task<MainScreenWorkflowResult> RunScanAsync(CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class FakeReportFileSaveService : IReportFileSaveService
    {
        public ReportFileSaveResult Save(string defaultFileName, string extension, string filter, string content)
            => ReportFileSaveResult.Saved;
    }

    private sealed class FakeDiagnosticLogsFolderService : IDiagnosticLogsFolderService
    {
        public string ResolveEffectiveFolderPath(string? customFolderPath)
            => string.IsNullOrWhiteSpace(customFolderPath) ? "C:/logs/default" : customFolderPath;

        public bool OpenFolder(string folderPath) => true;
    }

    private sealed class FakeOfficialSourceLauncher : IOfficialSourceLauncher
    {
        public bool Open(Uri uri) => true;
    }
}
