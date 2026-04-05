# DriverGuardian — синхронизированный план завершения до production

Дата синхронизации: 2026-04-05
Источник: фактическая реализация после Mirage + release/readiness assessment.

## 0) Что уже закрыто в коде

1. Runtime и основной workflow собраны end-to-end: scan → recommendation → official source resolution → history/report → audit.
2. Аудит персистентный: append-only + rotation/retention + correlation/event-code.
3. UI поддерживает manual handoff на официальный источник (main action + recommendation-level action).
4. Safety policy для URL официальных источников включена (доверенные домены, блок local/IP и небезопасных схем).
5. CI quality gates включены: tests, coverage gate, formatting/analyzers, vulnerability critical gate, publish artifact.
6. Snapshot-покрытие провайдеров внешне вынесено и усилено тестами на fixture coverage.

## 1) Что остаётся до public-release готовности

### 1.1 Provider depth (главный незакрытый риск)

Текущее состояние:
- Есть online-провайдер official catalog + fallback/runtime-обвязка.
- Базовый runtime всё ещё ограничен по полноте real-world metadata/coverage.

Что нужно сделать:
1. Доработать online provider до стабильного уровня (более точная выборка и ранжирование, прозрачные telemetry-сигналы качества).
2. Добавить минимум ещё один production-grade официальный источник (OEM/API/official feed) с детерминированными тестами.
3. Зафиксировать baseline/target по покрытию провайдеров на репрезентативной выборке hardware-id.

### 1.2 Release governance / runbook formalization

Текущее состояние:
- Для dev-pilot готовность высокая, но формальный go/no-go пакет ещё не стандартизован.

Что нужно сделать:
1. Оформить строгий release checklist (owners, sign-off, rollback, freeze criteria).
2. Привязать checklist к артефактам валидации и CI outcome.
3. Синхронизировать release notes/channel naming (dev-preview vs public-ready).

### 1.3 Validation follow-ups

Статус:
1. `WIN-VAL-2026-04-05-01` — ✅ Closed (runbook для restricted WMI ACL добавлен).
2. `WIN-VAL-2026-04-05-02` — ✅ Closed (offline UX copy + report phrasing синхронизированы).

## 2) План закрытия долгов по приоритетам

## P0 (текущий спринт)
1. Завершить provider-depth минимум (усиление live provider + ещё 1 production-grade официальный источник).
2. Финально синхронизировать release-facing документы и DoD-артефакты под текущее состояние ветки.

## P1 (следом)
3. [x] Закрепить formal go/no-go checklist и owners. (см. `docs/Release-GoNoGo-Checklist.md`)
4. [x] Добавить повторяемый pre-release smoke protocol (15–20 минут) в обязательный ритуал перед публикацией. (см. `docs/runbooks/Pre-Release-Smoke-Protocol.md`)

## 3) Definition of Done для перехода из dev-pilot к public-ready

1. Provider coverage демонстрирует устойчивый прирост на согласованном benchmark-наборе.
2. Все validation follow-up issue закрыты и подтверждены evidence. (на 2026-04-05 — выполнено)
3. Go/no-go checklist формализован, заполнен и подтверждён ответственными.
4. Документация не противоречит фактическому поведению runtime/UI/CI.

## 4) Рабочий трекер на текущую ветку

Актуальный краткий список задач для ветки ведём в:
- `docs/Sprint-Remaining-Tasks-2026-04-05.md`.
