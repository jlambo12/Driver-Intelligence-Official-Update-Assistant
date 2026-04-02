using System.Globalization;
using WpfApplication = System.Windows.Application;
using System.Windows;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.History;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Application.Recommendations;
using DriverGuardian.Application.Reports;
using DriverGuardian.Application.Scanning;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Contracts.DriverInspection;
using DriverGuardian.Infrastructure.Audit;
using DriverGuardian.Infrastructure.Diagnostics;
using DriverGuardian.Infrastructure.History;
using DriverGuardian.Infrastructure.Settings;
using DriverGuardian.Infrastructure.Time;
using DriverGuardian.ProviderAdapters.Abstractions.Registry;
using DriverGuardian.ProviderAdapters.Official.Registry;
using DriverGuardian.SystemAdapters.Windows.DeviceDiscovery;
using DriverGuardian.SystemAdapters.Windows.DriverInspection;
using DriverGuardian.UI.Wpf.Services;
using DriverGuardian.UI.Wpf.ViewModels;

namespace DriverGuardian.UI.Wpf;

public partial class App : WpfApplication
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");

        var previewModeEnabled = IsPreviewModeEnabled(e.Args);
        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DriverGuardian");
        var logsDirectory = Path.Combine(appDataDirectory, "logs");
        ISettingsRepository settingsRepository = BuildSettingsRepository(previewModeEnabled, appDataDirectory);
        ILogFolderResolver logFolderResolver = new SettingsLogFolderResolver(settingsRepository, logsDirectory);
        IMainScreenWorkflow mainScreenWorkflow = previewModeEnabled
            ? new PreviewScenarioMainScreenWorkflow()
            : BuildProductionWorkflow(settingsRepository, logFolderResolver);

        var vm = new MainViewModel(
            mainScreenWorkflow,
            settingsRepository,
            new ReportFileSaveService(),
            new LogFolderOpenService(logFolderResolver));
        var window = new MainWindow { DataContext = vm };
        window.Show();
    }

    private static bool IsPreviewModeEnabled(string[] args)
        => args.Any(arg => string.Equals(arg, "--demo", StringComparison.OrdinalIgnoreCase));

    private static ISettingsRepository BuildSettingsRepository(bool previewModeEnabled, string appDataDirectory)
    {
        if (previewModeEnabled)
        {
            return new InMemorySettingsRepository();
        }

        var settingsFilePath = Path.Combine(appDataDirectory, "settings.json");

        return new JsonFileSettingsRepository(settingsFilePath);
    }

    private static IMainScreenWorkflow BuildProductionWorkflow(
        ISettingsRepository settingsRepository,
        ILogFolderResolver logFolderResolver)
    {
        IClock clock = new SystemClock();
        IDeviceDiscoveryService discovery = new WindowsDeviceDiscoveryService();
        IDriverMetadataInspector inspector = new WindowsDriverMetadataInspectorStub();
        IDriverInspectionOrchestrator inspectionOrchestrator = new DriverInspectionOrchestrator(inspector);
        IScanOrchestrator scanOrchestrator = new ScanOrchestrator(discovery, inspectionOrchestrator, clock);
        IRecommendationPipeline recommendationPipeline = new RecommendationPipeline();
        IProviderRegistry providerRegistry = new OfficialProviderRegistryStub();
        IProviderCatalogSummaryService providerSummaryService = new ProviderCatalogSummaryService(providerRegistry);
        IAuditWriter auditWriter = new InMemoryAuditWriter();
        IResultHistoryRepository resultHistoryRepository = new InMemoryResultHistoryRepository();
        IDiagnosticLogger diagnosticLogger = new FileDiagnosticLogger(settingsRepository, logFolderResolver);
        var openOfficialSourceActionEvaluator = new OpenOfficialSourceActionEvaluator();
        IShareableReportBuilder reportBuilder = new ShareableReportBuilder();

        return new MainScreenWorkflow(
            scanOrchestrator,
            recommendationPipeline,
            providerSummaryService,
            settingsRepository,
            auditWriter,
            resultHistoryRepository,
            diagnosticLogger,
            openOfficialSourceActionEvaluator,
            reportBuilder);
    }
}
