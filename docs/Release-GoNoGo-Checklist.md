# DriverGuardian — Release Go/No-Go Checklist

Версия чеклиста: 2026-04-05

## 1) Validation readiness

- [ ] Windows validation matrix PASS (CI + manual evidence).
- [ ] Все follow-up issues закрыты или имеют утверждённый workaround.
- [ ] Для restricted WMI сценария применён runbook: `docs/runbooks/Restricted-WMI-ACL-Runbook.md`.

## 2) Runtime and quality gates

- [ ] CI quality gates зелёные (tests, coverage, analyzers/format, vulnerability critical).
- [ ] Smoke publish artifact сформирован и проверен.
- [ ] Нет blocker-дефектов уровня release-stop.

## 3) Sign-off

- [ ] QA owner sign-off.
- [ ] Runtime owner sign-off.
- [ ] Product/release owner sign-off.
