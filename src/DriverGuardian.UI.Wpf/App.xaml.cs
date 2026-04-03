using System;
using System.Globalization;
using System.IO;
using System.Linq;
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
using DriverGuardian.Domain.Settings;
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

namespace DriverGuardian.UI.Wpf;

public partial class App : WpfApplication
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");

        var previewModeEnabled = IsPreviewModeEnabled(e.Args);
        ISettingsRepository settingsRepository = BuildSettingsRepository(previewModeEnabled);
        var defaultLogsDirectory = GetDefaultLogsDirectory();
        var diagnosticLogsFolderService = new DiagnosticLogsFolderService(defaultLogsDirectory);
        var appSettings = settingsRepository.GetAsync(CancellationToken.None).GetAwaiter().GetResult();
        IDiagnosticLogger diagnosticLogger = previewModeEnabled
            ? new NoOpDiagnosticLogger()
            : BuildDiagnosticLogger(appSettings, diagnosticLogsFolderService);
        IMainScreenWorkflow mainScreenWorkflow = previewModeEnabled
            ? new PreviewScenarioMainScreenWorkflow()
            : BuildProductionWorkflow(settingsRepository, diagnosticLogger);

        var vm = new MainViewModel(
            mainScreenWorkflow,
            settingsRepository,
            new ReportFileSaveService(),
            diagnosticLogsFolderService);
        var window = new MainWindow { DataContext = vm };
        window.Show();
    }


    private static IDiagnosticLogger BuildDiagnosticLogger(
        AppSettings appSettings,
        IDiagnosticLogsFolderService diagnosticLogsFolderService)
    {
        if (!appSettings.DiagnosticLogging.Enabled)
        {
            return new NoOpDiagnosticLogger();
        }

        var effectiveLogsDirectory = diagnosticLogsFolderService.ResolveEffectiveFolderPath(
            appSettings.DiagnosticLogging.CustomLogsFolderPath);
        return new FileDiagnosticLogger(effectiveLogsDirectory);
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
        var officialProviders = new[] { new OfficialProviderAdapterStub() };
        IRecommendationPipeline recommendationPipeline = new RecommendationPipeline(officialProviders);
        IProviderRegistry providerRegistry = new OfficialProviderRegistryStub(officialProviders);
        IProviderCatalogSummaryService providerSummaryService = new ProviderCatalogSummaryService(providerRegistry);
        IAuditWriter auditWriter = new InMemoryAuditWriter();
        var historyFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DriverGuardian",
            "result-history.json");
        IResultHistoryRepository resultHistoryRepository = new JsonFileResultHistoryRepository(historyFilePath);
        var openOfficialSourceActionEvaluator = new OpenOfficialSourceActionEvaluator();
        IShareableReportBuilder reportBuilder = new ShareableReportBuilder();

        return new MainScreenWorkflow(
            scanOrchestrator,
            recommendationPipeline,
            providerSummaryService,
            settingsRepository,
            diagnosticLogger,
            auditWriter,
            resultHistoryRepository,
            openOfficialSourceActionEvaluator,
            reportBuilder);
    }
}
