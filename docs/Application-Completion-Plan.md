# DriverGuardian — повторный аудит и план завершения до production

Дата повторной проверки: 2026-04-03

## 0) Что проверено в коде (после последних изменений)

1. UI и тесты нацелены на `net8.0-windows10.0.19041.0`, включён Windows targeting.
2. Сканирование устройств/драйверов в production-ветке реально ходит в WMI (`Win32_PnPEntity`, `Win32_PnPSignedDriver`) и возвращает partial/failed при ошибках доступа/платформы.
3. Основной workflow выполняет scan → recommendation → official source resolution → history/report → audit.
4. Official-source policy проверяет trust level, HTTPS и host-match.
5. Report builder уже поддерживает verification-блок, но в текущем payload verifications всегда пустые.

## 1) Критические пробелы, которые мешают «работало правильно»

### 1.1 Нет полноценного production-каталога источников драйверов
Сейчас runtime-провайдеры ограничены:
- curated snapshot с очень узким покрытием exact hardware-id;
- baseline provider, который по дизайну всегда возвращает пустые кандидаты.

Риск:
- большинство реальных устройств будет получать `insufficient evidence` или «нет рекомендаций» даже при существующих обновлениях.

Что нужно сделать:
1. Добавить реальные online-интеграции официальных источников (OEM/support, Microsoft Catalog API/скрейпинг через безопасный слой).
2. Ввести нормализацию hardware-id (включая совместимые ID и ранжирование соответствия).
3. Добавить provider-уровневые SLA: timeout, retry transient-only, circuit-breaker, кэш.

### 1.2 Нет пользовательского действия «Открыть официальный источник» (UI gap)
Система вычисляет `ApprovedOfficialSourceUrl`, но в UI нет команды/кнопки, которая реально открывает ссылку.

Риск:
- ключевой сценарий manual handoff обрывается на «информировании», без завершения действия пользователем.

Что нужно сделать:
1. Добавить ICommand в `MainViewModel` для открытия approved URL.
2. Добавить UI-кнопку с disabled-state и явной причиной блокировки.
3. Перед открытием запускать policy-проверку/санитизацию URI (https, allow-list доменов, запрет file:// и т.п.).

### 1.3 Verification-пайплайн не встроен в основной workflow
`PostInstallVerificationEvaluator` реализован, но не включён в `MainScreenWorkflow`.
В отчёт сейчас передаются пустые коллекции verification/validation.

Риск:
- приложение не может автоматически подтвердить результат ручной установки «до/после».

Что нужно сделать:
1. Хранить baseline snapshot для device при выдаче рекомендации.
2. На повторном scan вычислять outcome (`VerifiedChanged / PartiallyChanged / NoChangeDetected / DeviceMissing / InsufficientEvidence`).
3. Писать verification в историю, UI и экспортируемый отчёт.

### 1.4 Аудит неперсистентный
Используется `InMemoryAuditWriter`.

Риск:
- после перезапуска невозможно расследовать пользовательский кейс и восстановить цепочку действий.

Что нужно сделать:
1. Перейти на persistent audit store (JSONL или SQLite).
2. Добавить `correlation-id` на scan-session и пронести через все события.
3. Добавить ротацию/retention и экспорт диагностического пакета.

### 1.5 Недостаточная release-готовность (CI/CD + quality gates)
В текущем окружении тесты не подтверждены (зависимость от .NET SDK).

Что нужно сделать:
1. Настроить CI (build/test/publish win-x64).
2. Добавить quality gates (analyzers, formatting, coverage).
3. Добавить nightly smoke для preview/prod wiring.

## 2) Важные нефункциональные доработки

1. **Безопасность ссылок**: централизованный allow-list trusted доменов и telemetry по отклонённым URL.
2. **Наблюдаемость**: структурированные логи с event-code и корреляцией по сессии.
3. **UX деградаций**: чётко различать «безопасно ничего не делать» и «данных недостаточно из-за ошибки поставщика».
4. **Локализация**: вынести remaining hardcoded EN-тексты в resources для единообразия RU-first UX.

## 3) Рекомендуемый план реализации

### Phase A (1–2 спринта): завершить критический пользовательский поток
- Реальные provider integrations + trust normalization.
- Кнопка/команда «Открыть официальный источник» с безопасным открытием.
- Интеграция verification evaluator в основной workflow.

### Phase B: надежность и эксплуатация
- Persistent audit + diagnostics bundle.
- Retry/timeout/circuit-breaker.
- Улучшенный UX статусов ошибок/блокировок.

### Phase C: релиз
- CI/CD и repeatable publish.
- Security/privacy review.
- Release checklist + rollback-процедура.

## 4) Definition of Done (для «приложение завершено»)

1. Для большинства целевых устройств рекомендации объяснимы и воспроизводимы.
2. Manual handoff замкнут: пользователь может открыть официальный источник прямо из UI безопасным способом.
3. Post-install verification автоматически фиксирует результат после повторного скана.
4. Аудит/логи персистентны и пригодны для расследования.
5. CI стабильно зелёный, win-x64 publish воспроизводим.

## 5) Конкретный backlog на ближайший спринт

1. `IUrlLauncher` + `OpenOfficialSourceCommand` + кнопка в manual action секции.
2. `VerificationTrackingService`: baseline capture + compare-on-rescan + запись в history.
3. `JsonFileAuditWriter` (или SQLite) + correlation-id propagation.
4. Первый production provider (например, OEM support portal) + integration tests с фикстурами.
5. GitHub Actions: build/test/publish + артефакт `DriverGuardian.UI.Wpf.exe`.
