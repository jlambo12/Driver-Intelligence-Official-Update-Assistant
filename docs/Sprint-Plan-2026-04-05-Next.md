# DriverGuardian — план следующего sprint-итерационного цикла

Дата старта: 2026-04-05  
Цель: перейти от dev-preview стабильности к расширенному official-source coverage для public-ready.

## 1) Приоритеты

1. **Provider depth / source diversity (P0)**
   - Добавить второй live production-grade официальный online source кроме Windows Catalog.
   - Сохранить требования resilience: timeout/retry/transient-классификация/circuit protection.

2. **Provider quality signals (P1)**
   - Уточнить confidence rationale и evidence-note так, чтобы в отчёте было ясно, почему выбран источник.
   - Зафиксировать дополнительные тесты на fallback-поведение при частичной недоступности online источников.

3. **Release discipline continuity (P1)**
   - После каждой выполненной задачи обновлять sprint tracking-документ.
   - Держать `docs/Sprint-Remaining-Tasks-2026-04-05.md` синхронизированным со статусом в коде.

## 2) Backlog на ближайшие PR

1. `provider:add-microsoft-support-online-source` (P0)
2. `provider:resilience-regression-tests` (P1)
3. `docs:sync-sprint-remaining-after-each-merge` (P1)

## 3) Definition of Done для текущей итерации

- В runtime зарегистрирован минимум один новый live official source.
- Есть unit-тесты на success/retry/no-hints для нового source.
- Sprint tracker обновлён и отражает факт выполнения.
