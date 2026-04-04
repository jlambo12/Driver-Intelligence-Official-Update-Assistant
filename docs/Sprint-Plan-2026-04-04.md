# DriverGuardian — чёткий план спринта

Дата старта: 2026-04-04  
Длительность: 2 недели (10 рабочих дней)

## 1) Цель спринта

Закрыть главные риски перед релизной стабилизацией:
1. поднять качество рекомендаций за счёт расширения provider coverage;
2. убрать узкое место в аудите (append-only + ротация);
3. включить обязательные quality gates в CI.

## 2) Scope спринта

## In scope (обязательный минимум)

### Epic A — Provider coverage
- Вынести curated snapshot из кода в управляемый источник данных (json fixture / data file).
- Добавить 1 production-ready online provider (официальный источник) с timeout/retry/circuit-breaker профилем.
- Добавить integration-like тесты на ключевые категории hardware-id (PCI/USB, exact/normalized/fallback).

### Epic B — Audit scalability
- Перевести `JsonFileAuditWriter` на append-only модель (без полного reread/rewrite файла).
- Добавить политику ротации и retention.
- Сохранить обратную совместимость структуры записи (`OccurredAtUtc`, `Entry`).

### Epic C — Quality gates в CI
- Добавить шаг покрытия тестов и порог (минимум 70% для unit-test target; точный порог фиксируем в PR).
- Добавить обязательные analyzer/style checks.
- Добавить dependency/security scan (как минимум отчёт + fail on critical).

## Out of scope (не делаем в этом спринте)
- Большая переработка UX и copywriting всех экранов.
- Полноценная multi-provider федерация с большим количеством интеграций.
- Миграция на SQLite/telemetry backend (если append-only jsonl достаточно для текущей нагрузки).

## 3) Детальный план по неделям

## Week 1

### День 1–2
- Технический дизайн по Epic A/B/C.
- Утверждение контрактов и формата данных для externalized snapshot.

### День 3–4
- Реализация Epic B (append-only audit + ротация).
- Unit tests для новой логики writer/rotation.

### День 5
- PR + ревью + merge по Epic B.
- Smoke проверка, что workflow сохраняет аудит как раньше.

## Week 2

### День 6–7
- Реализация Epic A (provider data externalization + online provider wiring).
- Добавление integration-like tests для provider резолюции.

### День 8
- Реализация Epic C (coverage/analyzers/security checks в workflow).

### День 9
- Финальный стабилизационный проход: фикс флейков, документация, release notes draft.

### День 10
- Sprint demo + freeze + план следующего спринта.

## 4) Backlog (чётко в формате задач)

1. `provider:externalize-catalog-snapshot`
   - Создать data file для snapshot.
   - Убрать hardcoded map из `OfficialWindowsCatalogProviderAdapter`.

2. `provider:add-online-official-source`
   - Добавить адаптер с безопасной сетевой политикой.
   - Подключить через `OfficialProviderRuntimeFactory`.

3. `provider:integration-tests`
   - Покрыть сценарии exact/normalized/vendor-fallback/no-match.

4. `audit:append-only-writer`
   - Переписать `JsonFileAuditWriter` на append-only.
   - Сохранить текущий контракт записи.

5. `audit:rotation-retention`
   - Добавить ограничение размера/периода и очистку старых файлов.

6. `ci:coverage-gate`
   - Добавить расчёт покрытия и порог.

7. `ci:analyzers-style-gate`
   - Включить обязательную проверку analyzers/format.

8. `ci:security-scan`
   - Добавить dependency/security checks с падением на critical.

9. `docs:sync-roadmap-with-reality`
   - Обновить `docs/Application-Completion-Plan.md` и убрать устаревшие пункты.

## 5) Definition of Done (для этого спринта)

Спринт считается закрытым, если одновременно выполнено:
1. Провайдеры дают измеримо лучшее покрытие на тестовой выборке (заранее фиксируем baseline и target).
2. Аудит не перечитывает весь файл на каждую запись и поддерживает ротацию.
3. CI блокирует merge при падении tests/coverage/analyzers/security.
4. Документация roadmap синхронизирована с фактическим кодом.

## 6) Ежедневные метрики контроля

- % закрытия задач по Epic A/B/C.
- Кол-во проваленных CI прогонов по новым gate.
- Время выполнения workflow test job (до/после изменений).
- Доля provider-тестов с успешной резолюцией по fixture-набору.

## 7) Риски и что делаем заранее

1. **Флейки в сетевом провайдере**  
   Митигируем: deterministic fixtures + timeout/retry limits + circuit-breaker тесты.

2. **Удлинение CI из-за новых проверок**  
   Митигируем: кэширование и разделение job/stage.

3. **Регрессии в main workflow после аудита**  
   Митигируем: unit tests + smoke сценарий scan→recommendation→audit.
