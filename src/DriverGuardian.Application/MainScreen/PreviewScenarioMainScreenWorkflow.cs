using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.OfficialSources;

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
            0, 0, 0, 0, 0, 0, 0,
            "Предпросмотр стартового состояния без результатов анализа.",
            "ru-RU",
            Guid.Empty,
            new ReportExportPayload("driverguardian-preview-first-run", string.Empty, string.Empty),
            [],
            new OpenOfficialSourceActionResult(false, OfficialSourceResolutionOutcome.InsufficientEvidence, "Официальный источник недоступен до появления рекомендаций.", null, null),
            []);

    private static MainScreenWorkflowResult BuildNoActionableRecommendation()
        => new(
            6, 6, 0, 6, 4, 0, 0,
            "Возврат для проверки не требуется: рекомендаций для ручной установки нет.",
            "ru-RU",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            new ReportExportPayload("driverguardian-preview-no-action", "Предпросмотр: безопасных рекомендаций не найдено.", "# DriverGuardian Preview\n\nБезопасных рекомендаций не найдено."),
            [
                new RecommendationDetailResult(
                    "Сетевой адаптер Intel Ethernet",
                    "PCI\\VEN_8086&DEV_51A3",
                    2,
                    false,
                    "Официальные источники не предоставили подтверждённого более нового пакета драйвера.",
                    "31.0.101.4577",
                    "Intel",
                    null,
                    null,
                    null,
                    null,
                    OfficialSourceResolutionOutcome.InsufficientEvidence,
                    false,
                    false,
                    false,
                    "Проверка после установки не требуется.")
            ],
            new OpenOfficialSourceActionResult(false, OfficialSourceResolutionOutcome.InsufficientEvidence, "Нет доступных рекомендаций для перехода к источнику.", null, null),
            []);

    private static MainScreenWorkflowResult BuildRecommendationWithLimitedEvidence()
        => new(
            5, 5, 1, 4, 4, 0, 1,
            "Для 1 устройства ожидается ручная установка и возврат пользователя для проверки.",
            "ru-RU",
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            new ReportExportPayload("driverguardian-preview-limited-evidence", "Предпросмотр: рекомендация есть, но доказательная база ограничена.", "# DriverGuardian Preview\n\nРекомендация есть, но источник требует дополнительной проверки."),
            [
                new RecommendationDetailResult(
                    "Контроллер Realtek PCIe GbE",
                    "PCI\\VEN_10EC&DEV_8168",
                    0,
                    true,
                    "Найден кандидат версии 10.64.1120.2025, но доверие к ссылке источника не подтверждено автоматически.",
                    "10.63.1014.2024",
                    "Realtek",
                    "10.64.1120.2025",
                    "realtek",
                    null,
                    null,
                    OfficialSourceResolutionOutcome.InsufficientEvidence,
                    false,
                    true,
                    true,
                    "Ожидается возврат пользователя после ручной установки.")
            ],
            new OpenOfficialSourceActionResult(false, OfficialSourceResolutionOutcome.InsufficientEvidence, "Требуется ручная проверка официальности источника.", null, "Недостаточно подтверждённых признаков источника"),
            []);

    private static MainScreenWorkflowResult BuildRecommendationReadyForManualAction()
        => new(
            7, 7, 1, 6, 4, 1, 1,
            "Ожидается возврат для проверки по 1 устройству после ручной установки.",
            "ru-RU",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            new ReportExportPayload("driverguardian-preview-manual-ready", "Предпросмотр: рекомендация готова к безопасному ручному действию.", "# DriverGuardian Preview\n\nРекомендация готова к безопасному ручному действию."),
            [
                new RecommendationDetailResult(
                    "Видеокарта NVIDIA GeForce",
                    "PCI\\VEN_10DE&DEV_28A1",
                    0,
                    true,
                    "Официальная страница производителя подтверждена и доступна для ручного перехода.",
                    "552.22",
                    "NVIDIA",
                    "555.10",
                    "nvidia",
                    null,
                    new Uri("https://www.nvidia.com/download/index.aspx"),
                    OfficialSourceResolutionOutcome.DirectOfficialDriverPageConfirmed,
                    true,
                    true,
                    true,
                    "Ожидается возврат пользователя после ручной установки.")
            ],
            new OpenOfficialSourceActionResult(true, OfficialSourceResolutionOutcome.DirectOfficialDriverPageConfirmed, "Официальный источник подтверждён для ручного открытия.", "https://www.nvidia.com/download/index.aspx", null),
            []);

    private static MainScreenWorkflowResult BuildVerificationReturnGuidance()
        => new(
            4, 4, 1, 3, 4, 1, 1,
            "Пользователь выполнил ручную установку: требуется повторный анализ для проверки результата.",
            "ru-RU",
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            new ReportExportPayload("driverguardian-preview-verification", "Предпросмотр: состояние возврата после ручной установки.", "# DriverGuardian Preview\n\nСценарий возврата для проверки после ручной установки."),
            [
                new RecommendationDetailResult(
                    "Графический адаптер Intel",
                    "PCI\\VEN_8086&DEV_A0F0",
                    0,
                    true,
                    "Ранее был предложен переход на официальную версию; ожидается подтверждение после повторного анализа.",
                    "31.0.101.5330",
                    "Intel",
                    "31.0.101.5590",
                    "intel",
                    null,
                    new Uri("https://www.intel.com/content/www/us/en/download-center/home.html"),
                    OfficialSourceResolutionOutcome.VendorSupportPageConfirmed,
                    true,
                    true,
                    true,
                    "Пользователь сообщил о ручной установке. Нужен контрольный анализ.")
            ],
            new OpenOfficialSourceActionResult(true, OfficialSourceResolutionOutcome.VendorSupportPageConfirmed, "Официальный источник подтверждён.", "https://www.intel.com/content/www/us/en/download-center/home.html", null),
            [
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddMinutes(-45), RecentHistoryEntryKind.Scan, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 4, 4, 0, null, null),
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddMinutes(-42), RecentHistoryEntryKind.Recommendation, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 1, 1, 0, null, null)
            ]);

    private static MainScreenWorkflowResult BuildPopulatedHistoryAndExport()
        => new(
            9, 9, 2, 7, 4, 1, 2,
            "Есть активные и завершённые задачи возврата для проверки; ориентируйтесь на журнал истории.",
            "ru-RU",
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            new ReportExportPayload("driverguardian-preview-history-export", "Предпросмотр: история заполнена, отчёт готов к ручному сохранению.", "# DriverGuardian Preview\n\nИстория заполнена, отчёт готов к ручному сохранению."),
            [
                new RecommendationDetailResult("Видеокарта NVIDIA GeForce", "PCI\\VEN_10DE&DEV_1C8D", 0, true, "Подтверждён безопасный ручной путь через официальный каталог.", "536.67", "NVIDIA", "552.44", "nvidia", null, new Uri("https://www.nvidia.com/download/index.aspx"), OfficialSourceResolutionOutcome.DirectOfficialDriverPageConfirmed, true, true, true, "Ожидается возврат пользователя после ручной установки."),
                new RecommendationDetailResult("Графический адаптер Intel", "PCI\\VEN_8086&DEV_1911", 1, true, "Есть кандидат, но ссылка требует дополнительной ручной проверки.", "30.0.101.1191", "Intel", "31.0.101.5590", "intel", null, null, OfficialSourceResolutionOutcome.InsufficientEvidence, false, true, true, "Ожидается подтверждение после повторного анализа.")
            ],
            new OpenOfficialSourceActionResult(true, OfficialSourceResolutionOutcome.DirectOfficialDriverPageConfirmed, "Официальный источник для первого устройства подтверждён.", "https://www.nvidia.com/download/index.aspx", null),
            [
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddHours(-5), RecentHistoryEntryKind.Scan, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 8, 8, 0, null, null),
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddHours(-5).AddMinutes(2), RecentHistoryEntryKind.Recommendation, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 2, 2, 6, null, null),
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddHours(-4), RecentHistoryEntryKind.Verification, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 0, 0, 0, "skipped", "Пользователь перенёс ручную установку на позднее время."),
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddHours(-2), RecentHistoryEntryKind.Scan, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), 9, 9, 0, null, null),
                new RecentHistoryEntryResult(DateTimeOffset.UtcNow.AddHours(-2).AddMinutes(3), RecentHistoryEntryKind.Verification, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), 0, 0, 0, "passed", "Повторный анализ зафиксировал ожидаемую версию драйвера.")
            ]);
}
