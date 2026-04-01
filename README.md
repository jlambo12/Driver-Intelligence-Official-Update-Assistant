# DriverGuardian Foundation (Stage 0)

## Цель
Безопасный анализ драйверов в режиме рекомендаций: без автоустановки, без тихих инсталляций, без неофициальных источников.

## Модульная структура
- **DriverGuardian.Domain**: язык домена, инварианты, типизированные модели и value objects.
- **DriverGuardian.Contracts**: контракты для системного сканирования/инспекции и нормализации.
- **DriverGuardian.Application**: orchestration use-cases (scan, inspection, summary pipeline), без UI/Windows деталей.
- **DriverGuardian.Infrastructure**: аудит, настройки, часы, база для будущей SQLite-персистентности.
- **DriverGuardian.SystemAdapters.Windows**: изоляция Windows-специфичных механизмов (пока безопасные stub реализации).
- **DriverGuardian.ProviderAdapters.Abstractions**: контракты внешних официальных провайдеров.
- **DriverGuardian.ProviderAdapters.Official**: начальная plug-in реализация официального провайдера и registry.
- **DriverGuardian.UI.Wpf**: WPF оболочка, MVVM, DI bootstrap, локализация.
- **DriverGuardian.Tests.Unit**: инварианты и orchestration wiring.

## Границы и расширяемость
- UI не вызывает Windows API напрямую; только application-orchestrators.
- Рекомендации отделены от установки.
- Модель `SourceEvidence` и `CompatibilityAssessmentResult` закладывают проверяемость и confidence.
- `IProviderRegistry` + `IOfficialDriverProviderAdapter` обеспечивают pluggable official-source-first pipeline.
- `IAuditLogger` + `AuditEvent` обеспечивают основу аудита.

## Локализация (ru-RU first)
- Все пользовательские строки UI находятся в `.resx`: `Strings.resx` и `Strings.ru-RU.resx`.
- Доступ централизован через `ILocalizedTextProvider` и `LocalizedStrings`.
- Domain/Application слои остаются language-neutral.
- Добавление нового языка делается через новый `Strings.<culture>.resx` без рефакторинга архитектуры.

## Logging & Error Handling Foundation
- `IAppLogger` writes structured operational/diagnostic logs using typed `LogEntry`.
- `IAuditLogger` records typed `AuditLogEntry` for security/compliance-sensitive actions.
- `OperationContext` + `CorrelationId` provide cross-module trace linkage.
- `IErrorNormalizer` maps exceptions into `NormalizedAppError` with stable error codes and handling hints.
- `IMetadataSanitizer` enforces safe metadata logging to avoid leaking secrets.
- `ILogSink` / `IAuditSink` provide extensibility points for future file sinks and diagnostics UI stores.
