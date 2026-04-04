# Official Windows Catalog Provider — Matching Rules

This document captures the expected lookup behavior for `OfficialWindowsCatalogProviderAdapter`.

## Match priority
1. **Exact match** by hardware id (`High` confidence).
2. **Normalized match** for supported buses (`Medium` confidence):
   - `PCI\\VEN_xxxx&DEV_yyyy`
   - `USB\\VID_xxxx&PID_yyyy`
3. **Vendor fallback** within the same bus family (`Low` confidence):
   - PCI requests may fallback only by `VEN_` within PCI snapshot entries.
   - USB requests may fallback only by `VID_` within USB snapshot entries.
4. If no candidate can be resolved, return success with an empty candidate list.

## Guardrails
- No cross-bus vendor fallback is allowed (PCI -> USB or USB -> PCI).
- Empty/whitespace hardware ids are ignored.
- Snapshot load failures (I/O or malformed JSON) must degrade to an empty catalog and must not throw from type initialization.
