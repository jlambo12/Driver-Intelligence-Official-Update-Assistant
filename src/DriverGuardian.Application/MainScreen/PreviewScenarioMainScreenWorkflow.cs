using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Application.MainScreen;

public enum PreviewScenarioId
{
    FirstRunPreScan = 0,
    NoActionableRecommendation = 1,
    RecommendationWithLimitedEvidence = 2,
    RecommendationReadyForManualAction = 3,
    VerificationReturnGuidance = 4,
    PopulatedHistoryAndExport = 5
}

public sealed class PreviewScenarioMainScreenWorkflow : IMainScreenWorkflow
{
    private readonly Dictionary<PreviewScenarioId, MainScreenWorkflowResult> _scenarioResults;

    public PreviewScenarioMainScreenWorkflow()
    {
        _scenarioResults = new Dictionary<PreviewScenarioId, MainScreenWorkflowResult>
        {
            [PreviewScenarioId.NoActionableRecommendation] = BuildNoActionableRecommendation(),
            [PreviewScenarioId.RecommendationWithLimitedEvidence] = BuildRecommendationWithLimitedEvidence(),
            [PreviewScenarioId.RecommendationReadyForManualAction] = BuildRecommendationReadyForManualAction(),
            [PreviewScenarioId.VerificationReturnGuidance] = BuildVerificationReturnGuidance(),
            [PreviewScenarioId.PopulatedHistoryAndExport] = BuildPopulatedHistoryAndExport()
        };
    }

    public PreviewScenarioId SelectedScenarioId { get; private set; } = PreviewScenarioId.FirstRunPreScan;

    public IReadOnlyList<PreviewScenarioId> AvailableScenarios { get; } =
    [
        PreviewScenarioId.FirstRunPreScan,
        PreviewScenarioId.NoActionableRecommendation,
        PreviewScenarioId.RecommendationWithLimitedEvidence,
        PreviewScenarioId.RecommendationReadyForManualAction,
        PreviewScenarioId.VerificationReturnGuidance,
        PreviewScenarioId.PopulatedHistoryAndExport
    ];

    public void SelectScenario(PreviewScenarioId scenarioId)
    {
        SelectedScenarioId = scenarioId;
    }

    public Task<MainScreenWorkflowResult> RunScanAsync(CancellationToken cancellationToken)
    {
        if (SelectedScenarioId == PreviewScenarioId.FirstRunPreScan)
        {
            return Task.FromResult(CreatePlaceholderResult());
        }

        return Task.FromResult(_scenarioResults[SelectedScenarioId]);
    }

    private static MainScreenWorkflowResult CreatePlaceholderResult()
        => new(
            DiscoveredDeviceCount: 0,
            InspectedDriverCount: 0,
            RecommendedCount: 0,
            NotRecommendedCount: 0,
            ProviderCount: 0,
            ManualHandoffReadyCount: 0,
            ManualHandoffUserActionCount: 0,
            VerificationSummary: "Предпросмотр стартового состояния без результатов анализа.",
            UiCulture: "ru-RU",
            ScanSessionId: Guid.Empty,
            ReportExportPayload: new ReportExportPayload("driverguardian-preview-first-run", string.Empty, string.Empty),
            RecommendationDetails: [],
            OfficialSourceAction: new OpenOfficialSourceActionResult(false, "Официальный источник недоступен до появления рекомендаций.", null, null),
            RecentHistory: []);

    private static MainScreenWorkflowResult BuildNoActionableRecommendation()
        => new(
            DiscoveredDeviceCount: 6,
            InspectedDriverCount: 6,
            RecommendedCount: 0,
            NotRecommendedCount: 6,
            ProviderCount: 4,
            ManualHandoffReadyCount: 0,
            ManualHandoffUserActionCount: 0,
            VerificationSummary: "Возврат для проверки не требуется: рекомендаций для ручной установки нет.",
            UiCulture: "ru-RU",
            ScanSessionId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ReportExportPayload: new ReportExportPayload(
                "driverguardian-preview-no-action",
                "Предпросмотр: безопасных рекомендаций не найдено.",
                "# DriverGuardian Preview\n\nБезопасных рекомендаций не найдено."),
            RecommendationDetails:
            [
                new RecommendationDetailResult(
                    "PCI\\VEN_8086&DEV_51A3",
                    false,
                    "Официальные источники не предоставили подтверждённого более нового пакета драйвера.",
                    "31.0.101.4577",
                    "Intel",
                    null,
                    false,
                    false,
                    false,
                    "Проверка после установки не требуется.")
            ],
            OfficialSourceAction: new OpenOfficialSourceActionResult(false, "Нет доступных рекомендаций для перехода к источнику.", null, null),
            RecentHistory: []);

    private static MainScreenWorkflowResult BuildRecommendationWithLimitedEvidence()
        => new(
            DiscoveredDeviceCount: 5,
            InspectedDriverCount: 5,
            RecommendedCount: 1,
            NotRecommendedCount: 4,
            ProviderCount: 4,
            ManualHandoffReadyCount: 0,
            ManualHandoffUserActionCount: 1,
            VerificationSummary: "Для 1 устройства ожидается ручная установка и возврат пользователя для проверки.",
            UiCulture: "ru-RU",
            ScanSessionId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ReportExportPayload: new ReportExportPayload(
                "driverguardian-preview-limited-evidence",
                "Предпросмотр: рекомендация есть, но доказательная база ограничена.",
                "# DriverGuardian Preview\n\nРекомендация есть, но источник требует дополнительной проверки."),
            RecommendationDetails:
            [
                new RecommendationDetailResult(
                    "PCI\\VEN_10EC&DEV_8168",
                    true,
                    "Найден кандидат версии 10.64.1120.2025, но доверие к ссылке источника не подтверждено автоматически.",
                    "10.63.1014.2024",
                    "Realtek",
                    "10.64.1120.2025",
                    false,
                    true,
                    true,
                    "Ожидается возврат пользователя после ручной установки.")
            ],
            OfficialSourceAction: new OpenOfficialSourceActionResult(false, "Требуется ручная проверка официальности источника.", null, "Недостаточно подтверждённых признаков источника"),
            RecentHistory: []);

    private static MainScreenWorkflowResult BuildRecommendationReadyForManualAction()
        => new(
            DiscoveredDeviceCount: 7,
            InspectedDriverCount: 7,
            RecommendedCount: 1,
            NotRecommendedCount: 6,
            ProviderCount: 4,
            ManualHandoffReadyCount: 1,
            ManualHandoffUserActionCount: 1,
            VerificationSummary: "Ожидается возврат для проверки по 1 устройству после ручной установки.",
            UiCulture: "ru-RU",
            ScanSessionId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ReportExportPayload: new ReportExportPayload(
                "driverguardian-preview-manual-ready",
                "Предпросмотр: рекомендация готова к безопасному ручному действию.",
                "# DriverGuardian Preview\n\nРекомендация готова к безопасному ручному действию."),
            RecommendationDetails:
            [
                new RecommendationDetailResult(
                    "PCI\\VEN_10DE&DEV_28A1",
                    true,
                    "Официальная страница производителя подтверждена и доступна для ручного перехода.",
                    "552.22",
                    "NVIDIA",
                    "555.10",
                    true,
                    true,
                    true,
                    "Ожидается возврат пользователя после ручной установки.")
            ],
            OfficialSourceAction: new OpenOfficialSourceActionResult(true, "Официальный источник подтверждён для ручного открытия.", "https://www.nvidia.com/download/index.aspx", null),
            RecentHistory: []);

    private static MainScreenWorkflowResult BuildVerificationReturnGuidance()
        => new(
            DiscoveredDeviceCount: 4,
            InspectedDriverCount: 4,
            RecommendedCount: 1,
            NotRecommendedCount: 3,
            ProviderCount: 4,
            ManualHandoffReadyCount: 1,
            ManualHandoffUserActionCount: 1,
            VerificationSummary: "Пользователь выполнил ручную установку: требуется повторный анализ для проверки результата.",
            UiCulture: "ru-RU",
            ScanSessionId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            ReportExportPayload: new ReportExportPayload(
                "driverguardian-preview-verification",
                "Предпросмотр: состояние возврата после ручной установки.",
                "# DriverGuardian Preview\n\nСценарий возврата для проверки после ручной установки."),
            RecommendationDetails:
            [
                new RecommendationDetailResult(
                    "PCI\\VEN_8086&DEV_A0F0",
                    true,
                    "Ранее был предложен переход на официальную версию; ожидается подтверждение после повторного анализа.",
                    "31.0.101.5330",
                    "Intel",
                    "31.0.101.5590",
                    true,
                    true,
                    true,
                    "Пользователь сообщил о ручной установке. Нужен контрольный анализ.")
            ],
            OfficialSourceAction: new OpenOfficialSourceActionResult(true, "Официальный источник подтверждён.", "https://www.intel.com/content/www/us/en/download-center/home.html", null),
            RecentHistory:
            [
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddMinutes(-45), RecentHistoryEntryKind.Scan, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 4, 4, 0, null, null),
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddMinutes(-42), RecentHistoryEntryKind.Recommendation, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 1, 1, 0, null, null)
            ]);

    private static MainScreenWorkflowResult BuildPopulatedHistoryAndExport()
        => new(
            DiscoveredDeviceCount: 9,
            InspectedDriverCount: 9,
            RecommendedCount: 2,
            NotRecommendedCount: 7,
            ProviderCount: 4,
            ManualHandoffReadyCount: 1,
            ManualHandoffUserActionCount: 2,
            VerificationSummary: "Есть активные и завершённые задачи возврата для проверки; ориентируйтесь на журнал истории.",
            UiCulture: "ru-RU",
            ScanSessionId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
            ReportExportPayload: new ReportExportPayload(
                "driverguardian-preview-history-export",
                "Предпросмотр: история заполнена, отчёт готов к ручному сохранению.",
                "# DriverGuardian Preview\n\nИстория заполнена, отчёт готов к ручному сохранению."),
            RecommendationDetails:
            [
                new RecommendationDetailResult(
                    "PCI\\VEN_10DE&DEV_1C8D",
                    true,
                    "Подтверждён безопасный ручной путь через официальный каталог.",
                    "536.67",
                    "NVIDIA",
                    "552.44",
                    true,
                    true,
                    true,
                    "Ожидается возврат пользователя после ручной установки."),
                new RecommendationDetailResult(
                    "PCI\\VEN_8086&DEV_1911",
                    true,
                    "Есть кандидат, но ссылка требует дополнительной ручной проверки.",
                    "30.0.101.1191",
                    "Intel",
                    "31.0.101.5590",
                    false,
                    true,
                    true,
                    "Ожидается подтверждение после повторного анализа.")
            ],
            OfficialSourceAction: new OpenOfficialSourceActionResult(true, "Официальный источник для первого устройства подтверждён.", "https://www.nvidia.com/download/index.aspx", null),
            RecentHistory:
            [
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddHours(-5), RecentHistoryEntryKind.Scan, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 8, 8, 0, null, null),
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddHours(-5).AddMinutes(2), RecentHistoryEntryKind.Recommendation, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 2, 2, 6, null, null),
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddHours(-4), RecentHistoryEntryKind.Verification, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 0, 0, 0, "skipped", "Пользователь перенёс ручную установку на позднее время."),
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddHours(-2), RecentHistoryEntryKind.Scan, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), 9, 9, 0, null, null),
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddHours(-2).AddMinutes(3), RecentHistoryEntryKind.Verification, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), 0, 0, 0, "passed", "Повторный анализ зафиксировал ожидаемую версию драйвера.")
            ]);
}
