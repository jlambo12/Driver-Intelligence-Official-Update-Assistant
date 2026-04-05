# Restricted WMI ACL Runbook (WIN-VAL-2026-04-05-01)

Дата: 2026-04-05  
Owner: Platform QA + Core Runtime

## Цель

Быстро отличать policy-driven ограничения WMI ACL от реальной поломки runtime DriverGuardian.

## Когда применять

- Scan выполняется с `Failed` или `Partial` статусом в средах с ограниченными правами.
- В audit/diagnostic логах есть индикаторы access denied по WMI namespace/queries.

## Ожидаемое поведение приложения (degraded-as-designed)

При restricted ACL **приложение не должно падать**. Ожидаемая деградация:

1. Scan завершён как `Partial` или `Failed` в зависимости от доли недоступных источников.
2. В issue-stream фиксируются WMI access-related ошибки.
3. UI остаётся интерактивным; пользователь видит объяснимый degraded state.
4. Workflow/history/audit сохраняются.

## Операторский quick-check (5–10 минут)

1. Проверить контекст запуска (standard/admin user, service account, policy scope).
2. Повторить scan и зафиксировать timestamp.
3. Проверить диагностический лог и audit (наличие access-denied следов, отсутствие crash).
4. Сверить финальный execution status (`Completed/Partial/Failed`) с объёмом недоступных WMI данных.
5. Проверить, что UI показывает контролируемую деградацию, а не «тихий успех».

## Диагностика WMI ACL

1. Проверить namespace-права для `root\cimv2` и связанных пространств, которые использует runtime.
2. Проверить локальные/доменные GPO, влияющие на WMI/DCOM доступ.
3. Подтвердить, что блокировка воспроизводима на том же policy profile.
4. Зафиксировать: кто, где, когда, какой namespace, какая ошибка доступа.

## Support handoff checklist

Перед эскалацией приложить:

- Версию приложения и коммит сборки;
- Windows edition/version/build;
- User context (standard/admin/service);
- Фрагмент audit + diagnostic log вокруг scan-сессии;
- Итоговый статус scan и список issue-кодов;
- Явное заключение: policy-driven ACL limitation **или** suspected runtime defect.

## Критерии закрытия инцидента как "не баг runtime"

- Поведение соответствует expected degraded path (без crash);
- Есть подтверждённый ACL/policy фактор;
- Рекомендовано действие по инфраструктуре (ACL/GPO) и повторный scan после изменения политики.

## Связанные артефакты

- Validation issue: `docs/validation-evidence/windows/issues/WIN-VAL-2026-04-05-01.md`
- Validation matrix: `docs/Windows-Validation-Matrix.md`
- Release checklist link: `docs/Release-GoNoGo-Checklist.md`
