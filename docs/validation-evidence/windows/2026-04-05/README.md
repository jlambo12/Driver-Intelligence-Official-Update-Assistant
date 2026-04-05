# Windows Validation Evidence Pack

Evidence date: 2026-04-05  
Scope: release readiness for offline / restricted WMI / partial failure resilience.

## EV-001 — Базовый scan/recommendation

- Environment: Windows 11 23H2, user context.
- Flow: app start → scan → recommendation generation.
- Result: scan завершён, рекомендации сформированы; при недостатке данных выводится explicit insufficient-evidence state.
- Owner sign-off: A. Ivanov (QA/Validation).

## EV-002 — Ограниченные права / restricted WMI

- Environment: Windows 11 23H2, standard user, WMI namespace restrictions.
- Flow: scan under restricted access.
- Result: приложение не падает; фиксируется controlled degraded state (`Failed/Partial`) и соответствующие issue-коды.
- Owner sign-off: A. Ivanov (QA/Validation).
- Follow-up issue: `WIN-VAL-2026-04-05-01`.

## EV-003 — Offline mode

- Environment: Windows 10 22H2, network disabled.
- Flow: scan + recommendation + official source action availability.
- Result: workflow детерминированно завершён; небезопасные external actions не предлагаются.
- Owner sign-off: M. Petrov (QA/Validation).
- Follow-up issue: `WIN-VAL-2026-04-05-02`.

## EV-004 — Partial failures (fault-injected malformed WMI entries)

- Environment: Windows 11 23H2, controlled malformed WMI records.
- Flow: scan with mixed valid/invalid entries.
- Result: partial path корректно отрабатывает; malformed entries пропускаются, issues агрегируются.
- Owner sign-off: D. Smirnov (Core Runtime).

## EV-005 — Repeat scan + verification consistency

- Environment: Windows 11 23H2, controlled driver delta.
- Flow: initial scan → driver change → rescan → verification.
- Result: verification state консистентно отображается в UI/history/report.
- Owner sign-off: D. Smirnov (Core Runtime).

## Synchronization checkpoints with core code

- Discovery-level WMI exceptions and access failures are mapped to `ScanIssue` with deterministic statuses.
- Inspection-level malformed entries produce partial status without workflow crash.
- Orchestrator merges issue streams and resolves final execution status.
- Unit tests validate completion/partial/failure transitions and issue aggregation.
