using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.History;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Application.ProviderCatalog;
using DriverGuardian.Application.Recommendations;
using DriverGuardian.Application.Reports;
using DriverGuardian.Application.Scanning;
using DriverGuardian.Application.Verification;
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
using System.IO;

namespace DriverGuardian.Bootstrap.Runtime;

public static class ProductionRuntimeFactory
{
    public static ProductionRuntime Create(string localAppDataRoot)
    {
        var appDataDirectory = Path.Combine(localAppDataRoot, "DriverGuardian");
        var defaultLogsDirectory = Path.Combine(appDataDirectory, "Logs");
        var settingsFilePath = Path.Combine(appDataDirectory, "settings.json");
        var historyFilePath = Path.Combine(appDataDirectory, "result-history.json");
        var auditFilePath = Path.Combine(appDataDirectory, "audit-log.jsonl");
        var verificationBaselineFilePath = Path.Combine(appDataDirectory, "verification-baselines.json");

        ISettingsRepository settingsRepository = new JsonFileSettingsRepository(settingsFilePath);
        IDiagnosticLogger runtimeDiagnosticLogger = new SettingsDiagnosticLogger(settingsRepository, defaultLogsDirectory);
        IDiagnosticLogsFolderService logsFolderService = new DiagnosticLogsFolderService(defaultLogsDirectory);

        IClock clock = new SystemClock();
        IDeviceDiscoveryService discovery = new WindowsDeviceDiscoveryService();
        IDriverMetadataInspector inspector = new WindowsDriverMetadataInspector();
        IScanOrchestrator scanOrchestrator = new ScanOrchestrator(discovery, inspector, clock);

        var officialProviders = OfficialProviderRuntimeFactory.CreateRuntimeProviders();
        IRecommendationPipeline recommendationPipeline = new RecommendationPipeline(officialProviders);
        IProviderRegistry providerRegistry = new OfficialProviderRegistry(officialProviders);
        IProviderCatalogSummaryService providerSummaryService = new ProviderCatalogSummaryService(providerRegistry);

        IAuditWriter auditWriter = new JsonFileAuditWriter(auditFilePath);
        IResultHistoryRepository resultHistoryRepository = new JsonFileResultHistoryRepository(historyFilePath);
        IVerificationBaselineStore verificationBaselineStore = new JsonFileVerificationBaselineStore(verificationBaselineFilePath);

        var officialSourceResolutionService = new OfficialSourceResolutionService(officialProviders);
        var officialSourceActionService = new OfficialSourceActionService(
            officialSourceResolutionService,
            new OpenOfficialSourceActionEvaluator(),
            runtimeDiagnosticLogger);

        var resultAssembler = new MainScreenResultAssembler(
            new RecommendationDetailAssembler(),
            officialSourceActionService,
            new VerificationTrackingService(verificationBaselineStore, new PostInstallVerificationEvaluator()),
            new ShareableReportBuilder());

        var mainScreenWorkflow = new MainScreenWorkflow(
            scanOrchestrator,
            recommendationPipeline,
            providerSummaryService,
            settingsRepository,
            runtimeDiagnosticLogger,
            auditWriter,
            resultAssembler,
            new ScanSessionHistoryService(resultHistoryRepository));

        return new ProductionRuntime(settingsRepository, logsFolderService, mainScreenWorkflow, runtimeDiagnosticLogger);
    }
}

public sealed record ProductionRuntime(
    ISettingsRepository SettingsRepository,
    IDiagnosticLogsFolderService DiagnosticLogsFolderService,
    IMainScreenWorkflow MainScreenWorkflow,
    IDiagnosticLogger StartupLogger);
