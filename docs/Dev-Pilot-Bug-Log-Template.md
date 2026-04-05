# DriverGuardian — Dev Pilot Bug Log Template

Use this template for every bug found during the internal `dev-preview` pilot.

---

## 1) Basic info

- **Bug ID**: `DG-BUG-YYYYMMDD-XX`
- **Date/Time (UTC)**:
- **Tester**:
- **Build/Commit**:
- **Environment**:
  - Windows version/build:
  - Device model:
  - CPU/GPU:
  - Network mode: `Online / Offline`
  - User permissions: `Admin / Standard / Restricted WMI`

## 2) Classification

- **Severity**: `Critical / High / Medium / Low`
- **Priority**: `P0 / P1 / P2`
- **Area**:
  - `Scan`
  - `Recommendation`
  - `Official Source`
  - `Verification`
  - `Report Export`
  - `History/Audit`
  - `Settings`
  - `UI/Localization`
- **Reproducibility**: `Always / Often / Sometimes / Rare`

## 3) Summary

- **Title**:
- **Expected result**:
- **Actual result**:
- **User impact**:

## 4) Reproduction steps

1. 
2. 
3. 
4. 

- **Preconditions** (if any):
- **Data/setup used**:

## 5) Technical evidence (required)

- **Scan session ID / correlation_id**:
- **event_code(s) seen**:
- **Audit/log files attached**:
  - [ ] `audit-log.jsonl`
  - [ ] diagnostic logs folder export
  - [ ] report file (`.txt`/`.md`) if relevant
- **Screenshots/video attached**:
- **Crash/exception text** (if any):

## 6) Scope check

- **Regression?** `Yes / No / Unknown`
  - If yes, last known good build:
- **Affects offline mode?** `Yes / No / Unknown`
- **Affects restricted WMI mode?** `Yes / No / Unknown`
- **Safety risk (wrong/untrusted source exposure)?** `Yes / No`

## 7) Triage outcome

- **Owner**:
- **Target fix version**:
- **Root cause hypothesis**:
- **Fix plan**:
- **Validation plan after fix**:

## 8) Closure

- **Fix commit/PR**:
- **Retest result**: `Passed / Failed / Partial`
- **Retest notes**:
- **Closed by / date**:

---

## Quick copy block

```text
Bug ID:
Date/Time (UTC):
Tester:
Build/Commit:
Windows version/build:
Device model:
Severity/Priority:
Area:
Reproducibility:
Title:
Expected:
Actual:
Steps:
1)
2)
3)
correlation_id:
event_code(s):
Attached logs:
Owner:
```
