# DriverGuardian — Release Go/No-Go Checklist

Версия чеклиста: 2026-04-05 (formalized)
Назначение: единый артефакт решения о выпуске (dev-preview/public-ready).

## 0) Release context (заполняется перед проверкой)

- Channel: `dev-preview` / `public-ready`
- Release candidate tag/build: `__________`
- Freeze window (UTC): `from ______ to ______`
- Release manager (owner): `__________`
- Planned rollback owner: `__________`

## 1) Validation readiness

- [ ] Windows validation matrix PASS (CI + manual evidence): `docs/Windows-Validation-Matrix.md`.
- [ ] Все validation follow-up issues закрыты или имеют утверждённый workaround.
- [ ] Для restricted WMI сценария применён runbook: `docs/runbooks/Restricted-WMI-ACL-Runbook.md`.
- [ ] Pre-release smoke protocol (15–20 минут) выполнен и приложен отчёт: `docs/runbooks/Pre-Release-Smoke-Protocol.md`.

## 2) Runtime and quality gates

- [ ] CI quality gates зелёные (tests, coverage, analyzers/format, vulnerability critical).
- [ ] Smoke publish artifact сформирован и проверен.
- [ ] Нет blocker-дефектов уровня release-stop.
- [ ] Известные ограничения/risks зафиксированы в release notes.

## 3) Release governance

- [ ] Release notes синхронизированы с каналом (`dev-preview` vs `public-ready`).
- [ ] Freeze criteria соблюдены: нет незакрытых blocker/critical дефектов по release scope.
- [ ] Все обязательные evidence-артефакты приложены (validation, CI, smoke, bug-log ссылки).

## 4) Rollback readiness (обязательно до Go)

- [ ] Последний стабильный build/tag для отката зафиксирован: `__________`.
- [ ] Подготовлен rollback plan (канал, бинарь, инструкции, контактные лица).
- [ ] Проверена доступность rollback-артефактов (скачивание/подпись/целостность).
- [ ] План коммуникации rollback согласован (кто и как уведомляет).

## 5) Sign-off (No-Go при отсутствии любого из пунктов)

- [ ] QA owner sign-off: `__________` / дата `__________`.
- [ ] Runtime owner sign-off: `__________` / дата `__________`.
- [ ] Product/release owner sign-off: `__________` / дата `__________`.

## 6) Decision

- [ ] **GO**
- [ ] **NO-GO**
- Комментарий решения: `________________________________________`.
