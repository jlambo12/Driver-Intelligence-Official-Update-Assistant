# Stage 0 Architecture Note

## Module responsibilities

- `DriverGuardian.Domain`: pure domain records and invariants for device identity, hardware identifiers, installed driver snapshots, recommendations, scan sessions, and app settings.
- `DriverGuardian.Contracts`: boundary interfaces for discovery, inspection, and normalization contracts.
- `DriverGuardian.Application`: orchestration use-cases for scanning, inspection coordination, and recommendation pipeline entry point.
- `DriverGuardian.Infrastructure`: minimal in-memory and system-time implementations for cross-cutting primitives required in stage 0.
- `DriverGuardian.SystemAdapters.Windows`: Windows-specific adapter stubs that implement contract interfaces without accessing live system APIs.
- `DriverGuardian.ProviderAdapters.Abstractions`: provider registry abstraction for pluggable official sources.
- `DriverGuardian.ProviderAdapters.Official`: official provider registry stub implementation.
- `DriverGuardian.UI.Wpf`: Russian-first WPF shell using MVVM and resource-based UI strings.
- `DriverGuardian.Tests.Unit`: starter tests for domain invariants and scan orchestration wiring.

## Intentionally deferred

- Real Windows device and driver metadata access.
- Provider network logic and official catalog integration.
- Driver install/update/download workflows.
- Centralized logging, correlation IDs, diagnostics query systems, and file-backed observability.
- Broad error normalization and advanced hardening layers.
