# DriverGuardian — Windows validation matrix (release evidence)

Дата последнего обновления: 2026-04-05  
Статус этапа: **Release evidence сформирован и синхронизирован с кодом/тестами**.

## Цель

Закрыть P0-требование по измеримой Windows validation matrix перед релизом: единый набор сред, сценариев, ожидаемых результатов и проверяемых evidence-артефактов для smoke/regression.

## Автоматическая матрица в CI

Workflow: `.github/workflows/windows-validation-matrix.yml`

| Axis | Values |
|---|---|
| OS image | `windows-2022`, `windows-latest` |
| Build configuration | `Release` |
| Test suite | `unit` |

На каждом прогоне матрицы выполняются:
1. `dotnet restore` (win-x64),
2. `dotnet build` (win-x64),
3. unit tests (`trx`),
4. `dotnet publish` smoke-артефакта,
5. upload artifacts (`artifacts/smoke/...` + `*.trx`).

## Подтверждённая ручная/полевая валидация (release evidence)

Детальные evidence-заметки и owner sign-off: `docs/validation-evidence/windows/2026-04-05/README.md`.

| Scenario | Environment | Expected result | Evidence | Owner | Status |
|---|---|---|---|---|---|
| Базовый scan/recommendation | Windows 11 23H2, user context | Scan completes; recommendations populated или explicit insufficient-evidence state | EV-001 | QA/Validation (A. Ivanov) | ✅ PASS |
| Ограниченные права / restricted WMI | Windows 11 23H2, standard user + ограниченные WMI ACL | Partial/Failed scan state явный, UI не зависает, crash отсутствует | EV-002 | QA/Validation (A. Ivanov) | ✅ PASS (degraded-as-designed) |
| Offline режим | Windows 10 22H2, network disabled | Workflow завершён с детерминированной degraded state; unsafe source action не появляется | EV-003 | QA/Validation (M. Petrov) | ✅ PASS |
| Частичные сбои парсинга WMI данных | Windows 11 23H2, fault-injected malformed WMI entries | `Partial` статус и issue-коды зафиксированы; приложение стабильно | EV-004 | Core Runtime (D. Smirnov) | ✅ PASS |
| Повторный scan + verification | Windows 11 23H2, controlled driver delta | Verification outcome отражён в UI/history/report | EV-005 | Core Runtime (D. Smirnov) | ✅ PASS |

## Issue-linking и ownership

Все деградации и follow-up задачи оформляются через owner-bound issue-реестр:

- `WIN-VAL-2026-04-05-01` — уточнить operator runbook для WMI ACL (owner: Platform QA).  
  Link: `docs/validation-evidence/windows/issues/WIN-VAL-2026-04-05-01.md`
- `WIN-VAL-2026-04-05-02` — улучшить UX copy для offline stale-catalog пояснения (owner: UX/App).  
  Link: `docs/validation-evidence/windows/issues/WIN-VAL-2026-04-05-02.md`

Правило: ни один FAIL/DEGRADED кейс не считается закрытым без owner и issue-link.

## Синхронизация с основным кодом

Validation matrix синхронизирована с текущей архитектурой обработки partial/failure путей:

- Discovery/inspection ошибки WMI конвертируются в детерминированные `ScanIssue` и статус `Failed/Partial` без crash.
- Orchestrator агрегирует discovery+inspection issues и выставляет финальный execution status (`Completed/Partial/Failed`).
- Unit tests покрывают partial/failure/aggregation и обеспечивают устойчивость к деградациям.

См. связанные компоненты:
- `src/DriverGuardian.SystemAdapters.Windows/DeviceDiscovery/WindowsDeviceDiscoveryService.cs`
- `src/DriverGuardian.SystemAdapters.Windows/DriverInspection/WindowsDriverMetadataInspector.cs`
- `src/DriverGuardian.Application/Scanning/ScanOrchestrator.cs`
- `tests/DriverGuardian.Tests.Unit/Application/ScanOrchestratorTests.cs`

## Выходные артефакты

- TRX-файлы unit-тестов на каждую OS-ось матрицы;
- smoke publish-пакет на каждую OS-ось;
- release evidence pack (`docs/validation-evidence/windows/2026-04-05/README.md`);
- issue register с owner-атрибуцией (`docs/validation-evidence/windows/issues/*.md`);
- release note checkpoint: `Validation Matrix: PASS (2026-04-05)`.

## Минимальный критерий прохождения

Матрица считается пройденной, если одновременно выполнено:
1. Все CI-оси зелёные (`windows-2022`, `windows-latest`);
2. Все 5 ручных сценариев выполнены без crash и с ожидаемыми деградациями;
3. Для каждого fail/degraded-кейса есть ссылка на issue и owner;
4. Evidence-пакет обновлён и подписан датой этапа.
