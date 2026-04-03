using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.History;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Application.ProviderCatalog;
using DriverGuardian.Application.Recommendations;
using DriverGuardian.Application.Reports;
using DriverGuardian.Application.Scanning;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Contracts.DriverInspection;
using DriverGuardian.Infrastructure.Audit;
using DriverGuardian.Infrastructure.DiagnosticLogging;
using DriverGuardian.Infrastructure.History;
using DriverGuardian.Infrastructure.Settings;
using DriverGuardian.Infrastructure.Time;
using DriverGuardian.ProviderAdapters.Abstractions.Registry;
using DriverGuardian.ProviderAdapters.Official.Registry;
using DriverGuardian.SystemAdapters.Windows.DeviceDiscovery;
using DriverGuardian.SystemAdapters.Windows.DriverInspection;
using DriverGuardian.UI.Wpf.ViewModels;
using WpfApplication = System.Windows.Application;

namespace DriverGuardian.UI.Wpf;

public partial class App : WpfApplication
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");
        var defaultLogsDirectory = GetDefaultLogsDirectory();
        var previewModeEnabled = IsPreviewModeEnabled(e.Args);
        var startupLogger = BuildStartupDiagnosticLogger(previewModeEnabled, defaultLogsDirectory);

        try
        {
            ISettingsRepository settingsRepository = BuildSettingsRepository(previewModeEnabled);
            var diagnosticLogsFolderService = new DiagnosticLogsFolderService(defaultLogsDirectory);
            IMainScreenWorkflow mainScreenWorkflow = previewModeEnabled
                ? new PreviewScenarioMainScreenWorkflow()
                : BuildProductionWorkflow(settingsRepository, defaultLogsDirectory);

            var vm = new MainViewModel(
                mainScreenWorkflow,
                settingsRepository,
                new ReportFileSaveService(),
                diagnosticLogsFolderService);

            await vm.InitializeAsync();

            var window = new MainWindow { DataContext = vm };
            window.Show();
            await startupLogger.LogInfoAsync("app.startup.completed", "Primary startup flow completed.", CancellationToken.None);
        }
        catch (Exception ex)
        {
            await startupLogger.LogErrorAsync("app.startup.failed", "Primary startup flow failed. Recovery mode will be activated.", ex, CancellationToken.None);
            var recoveryStatus = $"Startup recovery mode: {ex.GetType().Name} — {ex.Message}";
            MessageBox.Show(
                $"Не удалось запустить приложение в production-режиме. Активирован recovery-режим (preview).{Environment.NewLine}{Environment.NewLine}{recoveryStatus}",
                "DriverGuardian startup recovery",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            var fallbackVm = new MainViewModel(
                new PreviewScenarioMainScreenWorkflow(),
                new InMemorySettingsRepository(),
                new ReportFileSaveService(),
                new DiagnosticLogsFolderService(GetDefaultLogsDirectory()));
            await fallbackVm.InitializeAsync();
            fallbackVm.ApplyStartupRecoveryStatus(recoveryStatus);
            var fallbackWindow = new MainWindow { DataContext = fallbackVm };
            fallbackWindow.Show();
            await startupLogger.LogWarningAsync("app.startup.recovery_mode.enabled", "Application is running in recovery preview mode after startup failure.", CancellationToken.None);
        }
    }

    private static string GetDefaultLogsDirectory()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DriverGuardian",
            "Logs");

    private static bool IsPreviewModeEnabled(string[] args)
        => args.Any(arg => string.Equals(arg, "--demo", StringComparison.OrdinalIgnoreCase));

    private static ISettingsRepository BuildSettingsRepository(bool previewModeEnabled)
    {
        if (previewModeEnabled)
        {
            return new InMemorySettingsRepository();
        }

        var settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DriverGuardian");
        var settingsFilePath = Path.Combine(settingsDirectory, "settings.json");

        return new JsonFileSettingsRepository(settingsFilePath);
    }

    private static IDiagnosticLogger BuildStartupDiagnosticLogger(bool previewModeEnabled, string defaultLogsDirectory)
    {
        if (previewModeEnabled)
        {
            return new NoOpDiagnosticLogger();
        }

        return new FileDiagnosticLogger(defaultLogsDirectory);
    }

    private static IMainScreenWorkflow BuildProductionWorkflow(
        ISettingsRepository settingsRepository,
        string defaultLogsDirectory)
    {
        IDiagnosticLogger runtimeDiagnosticLogger = new SettingsDiagnosticLogger(settingsRepository, defaultLogsDirectory);
        IClock clock = new SystemClock();
        IDeviceDiscoveryService discovery = new WindowsDeviceDiscoveryService();
        IDriverMetadataInspector inspector = new WindowsDriverMetadataInspector();
        IScanOrchestrator scanOrchestrator = new ScanOrchestrator(discovery, inspector, clock);
        var officialProviders = OfficialProviderRuntimeFactory.CreateRuntimeProviders();
        IRecommendationPipeline recommendationPipeline = new RecommendationPipeline(officialProviders);
        IProviderRegistry providerRegistry = new OfficialProviderRegistry(officialProviders);
        IProviderCatalogSummaryService providerSummaryService = new ProviderCatalogSummaryService(providerRegistry);
        IAuditWriter auditWriter = new InMemoryAuditWriter();
        var historyFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DriverGuardian",
            "result-history.json");
        IResultHistoryRepository resultHistoryRepository = new JsonFileResultHistoryRepository(historyFilePath);
        var openOfficialSourceActionEvaluator = new OpenOfficialSourceActionEvaluator();
        var recommendationDetailAssembler = new RecommendationDetailAssembler();
        var officialSourceResolutionService = new OfficialSourceResolutionService(officialProviders);
        var officialSourceActionService = new OfficialSourceActionService(officialSourceResolutionService, openOfficialSourceActionEvaluator, runtimeDiagnosticLogger);
        var reportPayloadFactory = new ReportPayloadFactory(new ShareableReportBuilder());
        var historyWriter = new HistoryWriter(resultHistoryRepository);
        var historySummarizer = new HistorySummarizer(resultHistoryRepository);

        return new MainScreenWorkflow(
            scanOrchestrator,
            recommendationPipeline,
            providerSummaryService,
            settingsRepository,
            runtimeDiagnosticLogger,
            auditWriter,
            recommendationDetailAssembler,
            officialSourceActionService,
            reportPayloadFactory,
            historyWriter,
            historySummarizer);
    }
}
