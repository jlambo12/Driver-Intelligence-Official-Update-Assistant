# DriverGuardian — незакрытые задачи спринта

Дата фиксации: 2026-04-05
Источник сверки: `docs/Sprint-Plan-2026-04-04.md` + `docs/Project-Release-Readiness-Assessment-2026-04-05.md`.

## Что осталось сделать в рамках текущего спринта

1. [ ] `provider:add-online-official-source`
   - Добавить **хотя бы одного реально live** production-ready провайдера официального источника.
   - Обязательные требования: timeout/retry/transient-policy/circuit-breaker.
   - Почему не закрыто: в runtime пока нет truly live metadata-rich provider; используется curated/snapshot + OEM handoff.

2. [x] `docs:sync-roadmap-with-reality`
   - Обновить roadmap-документы так, чтобы они не противоречили фактической реализации.
   - Минимум: синхронизировать `docs/Application-Completion-Plan.md` и stage-артефакты с текущим состоянием Mirage.
   - Закрыто: синхронизирован `docs/Application-Completion-Plan.md` с текущим состоянием (2026-04-05).

## Что рекомендуется закрыть параллельно (P1 из release assessment)

> Эти пункты не обязательно были изначально в backlog спринта, но мешают качественному завершению ветки и переходу к более широкому rollout.

3. [x] `validation:runbook-wmi-acl`
   - Issue `WIN-VAL-2026-04-05-01` закрыт: добавлен runbook `docs/runbooks/Restricted-WMI-ACL-Runbook.md` и ссылка в release checklist.

4. [ ] `validation:offline-ux-copy`
   - Закрыть issue `WIN-VAL-2026-04-05-02` (actionable copy для offline сценариев).

## Оперативный порядок выполнения в этой ветке

1. Сначала `provider:add-online-official-source` (самый большой риск для release quality).
2. Затем добить validation-долги (`WIN-VAL-2026-04-05-01/02`).
3. После этого синхронизировать документацию (`docs:sync-roadmap-with-reality`).

## Definition of Done для остатка спринта

- Live provider реально подключён в runtime и покрыт интеграционными/интеграционно-подобными тестами.
- Все validation follow-up issues закрыты документально и проверены.
- Roadmap-документы отражают фактическое состояние кода без устаревших противоречий.
