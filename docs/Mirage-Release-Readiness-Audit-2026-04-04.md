# DriverGuardian — полный аудит после обновления «Мираж»

Дата: 2026-04-04

## 1) Короткий вывод

Проект заметно продвинулся: основной продуктовый контур уже собран end-to-end (scan → recommendation → official source action → history/report/audit/verifications), в CI включены quality gates, в UI закрыт critical gap по открытию официального источника.

При этом до полноценного релиза остаются 3 класса рисков:
1. ограниченное покрытие провайдеров (мало реальных источников и узкий snapshot);
2. эксплуатационные риски на росте нагрузки и наблюдаемости;
3. release-hardening и platform validation (особенно на живой Windows-матрице).

## 2) Что именно уже «замирожено» и подтверждено в коде

### 2.1 Production runtime действительно боевой

`ProductionRuntimeFactory` подключает реальные файловые репозитории settings/history/audit/baselines, Windows discovery + driver inspector, recommendation pipeline, official source action и verification tracking в главный workflow.

Итог: это уже не stage-0 skeleton, а рабочая интеграция ключевых слоёв.

### 2.2 Main workflow закрывает полный сценарий

`MainScreenWorkflow` вызывается через `MainViewModel`, результат проходит в UI state, историю и отчётный payload. Это подтверждает, что «ядро сценария пользователя» уже связано.

### 2.3 UI-gap по official source закрыт

В `MainViewModel.Actions` есть `OpenOfficialSourceAsync` и `OpenRecommendationOfficialSource`, а в `ManualActionSectionView.xaml` есть кнопка `OpenOfficialSourceCommand` + отображение причины блокировки, если действие недоступно.

### 2.4 Sprint quality gates реально включены в CI

В workflow `windows-portable-build.yml` уже присутствуют:
- запуск unit tests;
- coverage gate >= 70%;
- `dotnet format --verify-no-changes` (style/analyzers);
- vulnerability scan с падением на Critical.

Итог: часть задач, ранее считавшихся «нужно сделать», уже закрыта.

### 2.5 Провайдерный слой расширен, но пока ограничен

Есть runtime-провайдеры:
- `OfficialOemSupportProviderAdapter` (OEM support portal handoff по правилам);
- `OfficialWindowsCatalogProviderAdapter` (curated snapshot + fallback);
- baseline-провайдер (возвращает пусто).

Snapshot вынесен в data-файл `windows-catalog-snapshot.json`, что лучше, чем hardcode, но фактический размер базы пока маленький (несколько записей).

## 3) Статус задач «Мираж» (по факту)

| Направление | Статус | Комментарий |
|---|---|---|
| Externalized snapshot | ✅ Сделано | Snapshot вынесен в `Data/windows-catalog-snapshot.json`. |
| Online/official provider | 🟡 Частично | OEM-портал есть, но не полноценная data-rich интеграция каталога/вендоров с широким покрытием. |
| Integration-like provider tests | ✅ Сделано | Есть набор unit/integration-like тестов для official adapters. |
| Audit append-only | ✅ Сделано | Запись через `FileMode.Append`, без полного rewrite файла. |
| Audit rotation/retention | ✅ Сделано | Есть ротация и удаление старых архивов (`maxArchiveFiles`). |
| CI coverage gate | ✅ Сделано | Порог 70% в workflow. |
| CI analyzers/style gate | ✅ Сделано | `dotnet format --verify-no-changes`. |
| CI security scan | ✅ Сделано | `dotnet list package --vulnerable`, fail on critical. |
| Roadmap/docs sync | 🟡 Частично | Старые документы уже частично устарели и противоречат текущему состоянию. |

## 4) Недочёты и что обязательно доделать до релиза

### P0 — блокеры релизной уверенности

1. **Расширить реальное покрытие провайдеров**
   - Увеличить размер и актуализацию каталога (не 5–10 записей, а рабочий coverage на целевые классы устройств).
   - Добавить минимум 1 «глубокий» provider с richer metadata (версии/даты/ссылки/доказательства) и контролем SLA.
   - Ввести KPI покрытия (например, доля устройств с actionable official-source рекомендацией).

2. **Сделать измеримую Windows validation matrix**
   - Smoke/regression на нескольких Windows-конфигурациях и типах железа.
   - Отдельные сценарии: offline/partial WMI failure/restricted permissions.

3. **Нормализовать продуктовые статусы и UX деградаций**
   - Явно развести пользовательские состояния: «безопасно ничего не делать», «недостаточно данных», «источник заблокирован политикой», «частичный сбой сканирования».
   - Для каждого состояния дать понятный next step в UI.

### P1 — сильно желательно до release candidate

4. **Усилить наблюдаемость и расследуемость**
   - Единый correlation-id сквозь scan/recommendation/source open/verification/audit.
   - Стандартизованные event codes для support-разборов.

5. **Актуализировать документы, чтобы команда не жила по старому плану**
   - Обновить/пересобрать `Application-Completion-Plan` как «Done vs Remaining».
   - Заморозить release-checklist отдельным документом с владельцами задач.

6. **Уточнить политику данных и обновления snapshot**
   - Кто и как обновляет snapshot между релизами.
   - Правила валидации качества источников перед обновлением данных.

### P2 — релизный hardening

7. **Release operations**
   - Версионирование артефактов + changelog discipline.
   - Формальный go/no-go чек-лист и rollback процедура.

8. **Нефункциональные метрики**
   - Бюджеты времени для scan/recommendation/report.
   - Лимиты на рост audit/log и проверка очистки ретеншена.

## 5) Оценка готовности продукта

### В процентах

**Текущая оценка: 78% готовности к релизу.**

### По 10-балльной шкале

**7.8 / 10**

### Как посчитано (взвешенная модель)

- Продуктовый core workflow — 25% (выполнено ~90%)
- Безопасность/политики источников — 15% (выполнено ~80%)
- Provider coverage и качество рекомендаций — 25% (выполнено ~60%)
- Quality gates/CI/CD — 15% (выполнено ~85%)
- Эксплуатация/наблюдаемость/release operations — 20% (выполнено ~70%)

Суммарно: ~78%.

## 6) Что считать «готово к релизу» (минимальный проходной критерий)

Проект можно выпускать как production-ready, когда одновременно выполнены условия:
1. На согласованной тестовой выборке устройств достигается целевой coverage recommendations/source-actions.
2. Есть стабильные Windows smoke/regression прогоны по матрице окружений.
3. UX-статусы деградаций однозначны и приводят к действию.
4. Документация и release checklist синхронизированы с фактическим кодом и CI-политикой.
5. Команда может провести расследование user-case по логам/аудиту без ручных догадок.

## 7) Практический фокус следующего спринта

1. Provider coverage KPI + расширение data source.
2. Release validation matrix (Windows-only сценарии).
3. Корреляция и event codes в аудите/логах.
4. Полная синхронизация roadmap-документов под текущее состояние «Мираж».

## Update (ветка реализации, 2026-04-04)

В текущей ветке дополнительно реализованы начальные шаги по пунктам 3 и 4:
1. Нормализация пользовательских статус-сообщений для сценариев недостатка доказательств и blocked official source.
2. Добавлены `correlation_id` и `event_code` в audit-события workflow для сквозной расследуемости.
