# DriverGuardian — текущее состояние проекта и ближайший план

Дата оценки: 2026-04-04

## 1) Executive summary

Проект уже перешёл от «stage-0 заглушек» к рабочему production wiring:
- есть реальный runtime composition (сканирование, рекомендации, резолв official source, история, аудит, verification);
- есть рабочий CI workflow для Windows build/test/publish;
- есть UI-команды для открытия проверенных официальных источников;
- есть персистентный аудит (JSONL) и базовые механизмы верификации «до/после».

Но до production-grade остаются критичные задачи:
1. расширить покрытие источников драйверов (сейчас сильно ограничено curated/snapshot-логикой);
2. усилить эксплуатационные характеристики (производительность записи аудита, retention/архивация, телеметрия и корреляция end-to-end);
3. закрыть release/quality контур (coverage/анализаторы/security checks как quality gates);
4. провести стабилизацию UX и текста статусов для кейсов «нет данных/частичный сбой/недостаточно доказательств».

## 2) Что уже реализовано (факт по коду)

### 2.1 Архитектура и слои

Сохранена модульная архитектура: Domain, Contracts, Application, Infrastructure, Windows adapters, provider adapters, WPF UI, unit tests. Это соответствует архитектурной записке stage-0, но фактическая реализация уже заметно продвинулась дальше «базовых заглушек». 

### 2.2 Runtime wiring (production)

`ProductionRuntimeFactory` в production-профиле связывает:
- файловые репозитории settings/history/audit/verification baselines;
- Windows discovery + Windows metadata inspector;
- recommendation pipeline + provider registry;
- MainScreenWorkflow и MainScreenResultAssembler с VerificationTrackingService.

Это означает, что verification и persistent audit уже входят в основной боевой путь выполнения.

### 2.3 Основной workflow

`MainScreenWorkflow` выполняет полный цикл:
1. scan;
2. recommendation;
3. сборку результата (включая official-source action + verifications + report payload);
4. запись истории и аудита.

Аудит пишет события `scan.completed`, `official_source.state`, `verification.summary`.

### 2.4 Official source в UI

В UI есть:
- `OpenOfficialSourceCommand` и обработчик `OpenOfficialSourceAsync`;
- валидация URL через безопасный validator;
- кнопки открытия official source в manual action секции и в recommendation секции.

Следовательно, ранее зафиксированный «UI gap: нет кнопки открыть официальный источник» уже закрыт.

### 2.5 Verification pipeline

`VerificationTrackingService` действительно вызывается в `MainScreenResultAssembler` и сохраняет baseline + формирует verification-результаты при повторных сканах. Это закрывает ранее отмеченный архитектурный пробел по интеграции verification в основной workflow.

### 2.6 CI/CD (минимально рабочий)

Есть workflow `.github/workflows/windows-portable-build.yml`, который делает restore/build/test/publish на `windows-latest` и выгружает artifacts + build logs.

## 3) Ключевые риски и ограничения сейчас

### 3.1 Ограниченное покрытие provider-слоя

`OfficialWindowsCatalogProviderAdapter` использует небольшой встроенный curated snapshot (несколько hardware-id), с fallback по vendor. `OfficialProviderAdapterBaseline` всегда возвращает пустые кандидаты. Это даёт базовую работоспособность, но покрытие реальных устройств и качество рекомендаций остаются ограниченными.

### 3.2 Производительность и масштабируемость аудита

`JsonFileAuditWriter` на каждую запись перечитывает весь файл, добавляет запись и переписывает файл целиком (`ReadAllLinesAsync` + `WriteAllLinesAsync`). Для короткой истории это ок, но при росте частоты событий/размера лога это станет узким местом (I/O и latency).

### 3.3 Неполный quality gate контур

CI есть, но в текущем workflow нет явно выделенных:
- покрытия тестов с порогом;
- статического анализа/стайл-чеков как mandatory gate;
- security/dependency scanning в pipeline.

### 3.4 Документация частично устарела

`docs/Application-Completion-Plan.md` содержит пункты, которые уже реализованы в коде (open official source, verification integration, persistent audit). Это создаёт риск неверной приоритизации и потери времени команды.

## 4) Что делать в ближайшее время (приоритетный план)

## P0 (срочно, ближайший спринт)

1. **Обновить source of truth по roadmap**
   - Синхронизировать `docs/Application-Completion-Plan.md` с фактическим состоянием кода.
   - Разделить «уже сделано» vs «осталось сделать».

2. **Усилить provider coverage**
   - Вынести curated snapshot в отдельный управляемый data source (обновляемый вне релиза).
   - Добавить как минимум один реальный online provider с безопасным rate-limit/retry/timeout профилем.
   - Добавить integration-like tests на типовые hardware-id категории.

3. **Сделать audit writer более эффективным**
   - Перейти на append-only запись JSONL без перечитывания всего файла.
   - Добавить политику ротации (size/date) и retention.

## P1 (следом)

4. **Закрыть эксплуатационный контур качества**
   - Добавить coverage отчёт + минимальный порог.
   - Включить analyzers/style checks как обязательные шаги CI.
   - Добавить dependency/security scanning.

5. **Улучшить UX для деградаций**
   - Нормализовать статусы «insufficient evidence / blocked / partial scan / no recommendation».
   - Убедиться, что для каждого сценария есть понятное действие для пользователя.

## P2 (стабилизация релиза)

6. **Release hardening**
   - Версионирование артефактов и changelog discipline.
   - Smoke-check после publish артефакта.
   - Формализованный release checklist (rollback, known issues, validation matrix).

## 5) Чек-лист принятия ближайшего этапа

Этап можно считать завершённым, если:
1. roadmap-документация не противоречит коду;
2. provider coverage расширено и подтверждено тестами;
3. аудит пишет append-only и не деградирует с ростом файла;
4. CI падает при нарушении quality gates (tests/coverage/analyzers/security);
5. UX-сообщения в критичных сценариях однозначно ведут пользователя к следующему действию.


---

См. также детальный спринт-план: `docs/Sprint-Plan-2026-04-04.md`.
