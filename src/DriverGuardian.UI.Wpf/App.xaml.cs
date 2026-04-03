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
using DriverGuardian.UI.Wpf.Services;
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

        try
        {
            var previewModeEnabled = IsPreviewModeEnabled(e.Args);
            ISettingsRepository settingsRepository = BuildSettingsRepository(previewModeEnabled);
            var defaultLogsDirectory = GetDefaultLogsDirectory();
            var diagnosticLogsFolderService = new DiagnosticLogsFolderService(defaultLogsDirectory);
            var diagnosticLogger = previewModeEnabled
                ? new NoOpDiagnosticLogger()
                : new FileDiagnosticLogger(defaultLogsDirectory);
            IMainScreenWorkflow mainScreenWorkflow = previewModeEnabled
                ? new PreviewScenarioMainScreenWorkflow()
                : BuildProductionWorkflow(settingsRepository, diagnosticLogger);

            var vm = new MainViewModel(
                mainScreenWorkflow,
                settingsRepository,
                new ReportFileSaveService(),
                diagnosticLogsFolderService);

            await vm.InitializeAsync();

            var window = new MainWindow { DataContext = vm };
            window.Show();
        }
        catch
        {
            var fallbackVm = new MainViewModel(
                new PreviewScenarioMainScreenWorkflow(),
                new InMemorySettingsRepository(),
                new ReportFileSaveService(),
                new DiagnosticLogsFolderService(GetDefaultLogsDirectory()));
            await fallbackVm.InitializeAsync();
            var fallbackWindow = new MainWindow { DataContext = fallbackVm };
            fallbackWindow.Show();
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

    private static IMainScreenWorkflow BuildProductionWorkflow(
        ISettingsRepository settingsRepository,
        IDiagnosticLogger diagnosticLogger)
    {
        IClock clock = new SystemClock();
        IDeviceDiscoveryService discovery = new WindowsDeviceDiscoveryService();
        IDriverMetadataInspector inspector = new WindowsDriverMetadataInspector();
        IDriverInspectionOrchestrator inspectionOrchestrator = new DriverInspectionOrchestrator(inspector);
        IScanOrchestrator scanOrchestrator = new ScanOrchestrator(discovery, inspectionOrchestrator, clock);
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
        var officialSourceActionService = new OfficialSourceActionService(officialSourceResolutionService, openOfficialSourceActionEvaluator);
        var reportPayloadFactory = new ReportPayloadFactory(new ShareableReportBuilder());
        var historyWriter = new HistoryWriter(resultHistoryRepository);
        var historySummarizer = new HistorySummarizer(resultHistoryRepository);

        return new MainScreenWorkflow(
            scanOrchestrator,
            recommendationPipeline,
            providerSummaryService,
            settingsRepository,
            diagnosticLogger,
            auditWriter,
            recommendationDetailAssembler,
            officialSourceActionService,
            reportPayloadFactory,
            historyWriter,
            historySummarizer);
    }
}
