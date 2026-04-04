# DriverGuardian — Windows validation matrix

Дата: 2026-04-04

## Цель

Закрыть P0-требование по измеримой Windows validation matrix перед релизом: единый набор сред, сценариев и ожидаемых результатов для smoke/regression.

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
3. unit tests,
4. `dotnet publish` smoke-артефакта.

## Ручная валидационная матрица (до RC)

| Scenario | Environment | Expected result |
|---|---|---|
| Базовый scan/recommendation | Windows 11 23H2 (user context) | Scan completes; recommendations populated or explicit insufficient-evidence state |
| Ограниченные права/частичный доступ WMI | Windows 11 (standard user + restricted policies) | Partial scan state is explicit; app remains responsive; no crash |
| Offline режим | Windows 10 22H2 (network disabled) | Workflow completes with deterministic degraded state; official source action remains safe |
| Повторный scan с verification | Windows 11 + controlled driver delta | Verification outcome reflected in UI/history/report |

## Выходные артефакты

- TRX-файлы unit-тестов на каждую OS-ось матрицы;
- smoke publish-пакет на каждую OS-ось;
- release note checkpoint: "Validation Matrix: PASS/FAIL".

## Минимальный критерий прохождения

Матрица считается пройденной, если:
1. Все CI-оси зелёные (`windows-2022`, `windows-latest`);
2. Все 4 ручных сценария выполнены без crash и с ожидаемыми деградациями;
3. Для каждого fail-кейса есть ссылка на issue и owner.
