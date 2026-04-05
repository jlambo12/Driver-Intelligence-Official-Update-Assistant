using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Domain.Settings;
using DriverGuardian.Infrastructure.DiagnosticLogging;
using DriverGuardian.Infrastructure.Settings;
using DriverGuardian.UI.Wpf.Localization;
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
            WorkflowGuidance = AppSettings.Default.WorkflowGuidance with { ShowPostInstallVerificationHints = false },
            ScanCoverage = new ScanCoveragePreferences(DeviceScanProfile.Comprehensive)
        };

        var repository = await CreateSettingsRepositoryAsync(settings);
        var viewModel = CreateMainViewModel(new StubMainScreenWorkflow(CreateResult()), repository);

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal(133, viewModel.SettingsSection.HistoryMaxEntries);
        Assert.False(viewModel.SettingsSection.ShowVerificationHints);
        Assert.Equal(ShareableReportFormat.PlainText, viewModel.SettingsSection.SelectedReportFormat.Value);
        Assert.Equal(DeviceScanProfile.Comprehensive, viewModel.SettingsSection.SelectedScanProfile.Value);
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

        var viewModel = CreateMainViewModel(new StubMainScreenWorkflow(result), await CreateSettingsRepositoryAsync(AppSettings.Default));
        await viewModel.InitializeAsync(CancellationToken.None);

        await ExecuteAsync(viewModel.ScanCommand);

        Assert.Equal(UiStrings.ReportExportStatusReady, viewModel.ReportSection.ReportExportStatusText);
        Assert.Single(viewModel.HistorySection.RecentHistory);
        Assert.True(viewModel.State.Results.HasScanData);
    }

    private static MainViewModel CreateMainViewModel(
        IMainScreenWorkflow workflow,
        ISettingsRepository settingsRepository,
        IOfficialSourceLauncher? launcher = null)
        => new(
            workflow,
            settingsRepository,
            new FakeReportFileSaveService(),
            new FakeDiagnosticLogsFolderService(),
            launcher ?? new FakeOfficialSourceLauncher());

    private static async Task<InMemorySettingsRepository> CreateSettingsRepositoryAsync(AppSettings settings)
    {
        var repository = new InMemorySettingsRepository();
        await repository.SaveAsync(settings, CancellationToken.None);
        return repository;
    }

    private static async Task ExecuteAsync(System.Windows.Input.ICommand command)
    {
        var task = ((DriverGuardian.UI.Wpf.Commands.AsyncRelayCommand)command).ExecuteAsync(null);
        await task;
    }


    [Fact]
    public async Task ScanAsync_WithLoopbackApprovedUrl_ShouldKeepOpenOfficialSourceDisabled()
    {
        var result = CreateResult(
            officialSourceAction: new OpenOfficialSourceActionResult(
                true,
                OfficialSourceResolutionOutcome.ConfirmedVendorSupportPage,
                OfficialSourceActionTarget.SourcePage,
                "status",
                "https://localhost/download",
                null));

        var viewModel = CreateMainViewModel(new StubMainScreenWorkflow(result), await CreateSettingsRepositoryAsync(AppSettings.Default));
        await viewModel.InitializeAsync(CancellationToken.None);

        await ExecuteAsync(viewModel.ScanCommand);

        Assert.False(viewModel.CanOpenOfficialSource);
    }

    [Fact]
    public async Task ScanAsync_WithHttpsNonLoopbackApprovedUrl_ShouldEnableOpenOfficialSource()
    {
        var result = CreateResult(
            officialSourceAction: new OpenOfficialSourceActionResult(
                true,
                OfficialSourceResolutionOutcome.ConfirmedVendorSupportPage,
                OfficialSourceActionTarget.SourcePage,
                "status",
                "https://www.microsoft.com/windows",
                null));

        var viewModel = CreateMainViewModel(new StubMainScreenWorkflow(result), await CreateSettingsRepositoryAsync(AppSettings.Default));
        await viewModel.InitializeAsync(CancellationToken.None);

        await ExecuteAsync(viewModel.ScanCommand);

        Assert.True(viewModel.CanOpenOfficialSource);
    }


    [Fact]
    public async Task OpenRecommendationOfficialSourceCommand_WithSafeUrl_ShouldLaunchSource()
    {
        var launcher = new FakeOfficialSourceLauncher();
        var result = CreateResult(
            recommendationDetails:
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
                    "hint",
                    "https://www.microsoft.com/windows")
            ]);

        var viewModel = CreateMainViewModel(new StubMainScreenWorkflow(result), await CreateSettingsRepositoryAsync(AppSettings.Default), launcher);
        await viewModel.InitializeAsync(CancellationToken.None);
        await ExecuteAsync(viewModel.ScanCommand);

        viewModel.OpenRecommendationOfficialSourceCommand.Execute("https://www.microsoft.com/windows");

        Assert.Equal("https://www.microsoft.com/windows", launcher.LastOpenedUri?.AbsoluteUri);
    }

    [Fact]
    public async Task OpenRecommendationOfficialSourceCommand_WithUnsafeUrl_ShouldNotLaunchSource()
    {
        var launcher = new FakeOfficialSourceLauncher();
        var viewModel = CreateMainViewModel(new StubMainScreenWorkflow(CreateResult()), await CreateSettingsRepositoryAsync(AppSettings.Default), launcher);
        await viewModel.InitializeAsync(CancellationToken.None);
        await ExecuteAsync(viewModel.ScanCommand);

        viewModel.OpenRecommendationOfficialSourceCommand.Execute("http://vendor.example/driver");

        Assert.Null(launcher.LastOpenedUri);
        Assert.Equal(UiStrings.OfficialSourceUrlUnavailable, viewModel.State.StatusText);
    }

    private static MainScreenWorkflowResult CreateResult(
        ReportExportPayload? reportPayload = null,
        IReadOnlyCollection<RecentHistoryEntryResult>? recentHistory = null,
        OpenOfficialSourceActionResult? officialSourceAction = null,
        IReadOnlyCollection<RecommendationDetailResult>? recommendationDetails = null)
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
            recommendationDetails ??
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
            officialSourceAction ?? new OpenOfficialSourceActionResult(true, OfficialSourceResolutionOutcome.ConfirmedVendorSupportPage, OfficialSourceActionTarget.SourcePage, "status", "https://example.com", null),
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
            => string.IsNullOrWhiteSpace(customFolderPath) ? "C:/logs/default" : customFolderPath.Trim();

        public bool OpenFolder(string folderPath) => true;
    }

    private sealed class FakeOfficialSourceLauncher : IOfficialSourceLauncher
    {
        public Uri? LastOpenedUri { get; private set; }

        public bool Open(Uri uri)
        {
            LastOpenedUri = uri;
            return true;
        }
    }
}
