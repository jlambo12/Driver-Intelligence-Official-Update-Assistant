# DriverGuardian — Pre-Release Smoke Protocol (15–20 min)

Версия: 2026-04-05
Цель: быстрый обязательный smoke-проход перед решением Go/No-Go.

## 1) Preconditions

1. Собран release candidate (dev-preview/public-ready).
2. CI quality gates по RC зелёные.
3. Тестовый стенд соответствует `docs/Windows-Validation-Matrix.md`.
4. Под рукой bug-log шаблон: `docs/Dev-Pilot-Bug-Log-Template.md`.

## 2) Smoke loop (пошагово)

1. Запустить приложение и убедиться, что старт без критических ошибок.
2. Выполнить scan устройств.
3. Проверить блок рекомендаций (есть результат, нет явных runtime ошибок).
4. Открыть official-source action минимум для одной рекомендации.
5. Экспортировать shareable report.
6. Выполнить повторный scan после имитации ручного действия/изменения.
7. Проверить history/audit-записи на корректность и отсутствие деградаций.

## 3) Pass/Fail критерии

## PASS

- Ни один шаг smoke loop не падает критически.
- UI не показывает blocker-level ошибок.
- Export report создаётся успешно.
- History/audit отражают выполненные действия.

## FAIL (Release-stop)

- Краш приложения или невозможность пройти базовый scan workflow.
- Official-source action недоступен или небезопасен.
- Report export не работает.
- Повторный scan/история/аудит работают некорректно.

## 4) Evidence to attach

1. Ссылка на RC build/tag.
2. Скриншот(ы) ключевых экранов smoke-прохода.
3. Ссылка на CI run.
4. Bug-log по всем отклонениям (если есть).

## 5) Output format (for checklist)

- Smoke status: PASS / FAIL
- Environment: Windows version + hardware profile
- Executor: имя
- Timestamp (UTC)
- Notes / найденные дефекты
