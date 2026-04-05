# DriverGuardian — незакрытые задачи спринта

Дата фиксации: 2026-04-05
Источник сверки: `docs/Sprint-Plan-2026-04-04.md` + `docs/Project-Release-Readiness-Assessment-2026-04-05.md`.

## Что осталось сделать в рамках текущего спринта

1. [x] `provider:add-online-official-source`
   - Добавить **хотя бы одного реально live** production-ready провайдера официального источника.
   - Обязательные требования: timeout/retry/transient-policy/circuit-breaker.
   - Закрыто: добавлен live-провайдер `OfficialWindowsCatalogOnlineProviderAdapter`, подключён в runtime factory и углублён многошаговой стратегией query hints + transient-классификацией.

2. [x] `docs:sync-roadmap-with-reality`
   - Обновить roadmap-документы так, чтобы они не противоречили фактической реализации.
   - Минимум: синхронизировать `docs/Application-Completion-Plan.md` и stage-артефакты с текущим состоянием Mirage.
   - Закрыто: синхронизирован `docs/Application-Completion-Plan.md` с текущим состоянием (2026-04-05).

## Что рекомендуется закрыть параллельно (P1 из release assessment)

> Эти пункты не обязательно были изначально в backlog спринта, но мешают качественному завершению ветки и переходу к более широкому rollout.

3. [x] `validation:runbook-wmi-acl`
   - Issue `WIN-VAL-2026-04-05-01` закрыт: добавлен runbook `docs/runbooks/Restricted-WMI-ACL-Runbook.md` и ссылка в release checklist.

4. [x] `validation:offline-ux-copy`
   - Issue `WIN-VAL-2026-04-05-02` закрыт: синхронизированы offline UX-copy и shareable report phrasing, добавлено actionable next-step guidance.

## Текущий статус

Все пункты текущего sprint tail закрыты в этой ветке.

## Definition of Done для остатка спринта

- [x] Live provider подключён в runtime и покрыт unit/интеграционно-подобными тестами.
- [x] Validation follow-up issues закрыты документально и проверены.
- [x] Roadmap-документы отражают фактическое состояние кода без устаревших противоречий.

---

## Обновление 2026-04-05 (старт следующей sprint-итерации)

Новый рабочий план: `docs/Sprint-Plan-2026-04-05-Next.md`.

### Текущий прогресс

1. [x] `provider:add-microsoft-support-online-source`
   - Добавлен online адаптер `OfficialMicrosoftSupportOnlineProviderAdapter`.
   - Подключён в `OfficialProviderRuntimeFactory`.
   - Добавлены unit-тесты на success/retry/no-hints.

2. [ ] `provider:resilience-regression-tests`
   - Расширить матрицу регрессионных сценариев partial outage между online провайдерами.

3. [ ] `docs:sync-sprint-remaining-after-each-merge`
   - Поддерживать этот документ в актуальном состоянии после каждого завершённого task/PR.
