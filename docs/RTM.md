# Requirements Traceability Matrix (RTM) — Cooldown.gg  
*Version: 1.0 — Updated 2025-12-06*  

This RTM connects **business goals**, **functional & non-functional requirements**, **WBS tasks**, **test cases**, and **DX-ID deferred issues** across all development phases.  
It serves as the single source of truth for system verification and future development planning.

---

# 1. Key to Columns

| Column | Meaning |
|--------|---------|
| **Req ID** | Unique requirement identifier (BRD/FR/NFR/DXR) |
| **Requirement Description** | What the system must do |
| **Type** | BR = Business, FR = Functional, NFR = Non-functional, DXR = Derived from DX-ID |
| **Origin** | Source document (BRD, PRD, Test Report DX-ID, etc.) |
| **WBS** | Work Breakdown Structure task(s) that implement the requirement |
| **Test Case(s)** | Test IDs validating the requirement |
| **DX-ID** | Linked deferred issue (if any) |
| **Status** | Required / Implemented / Tested / Verified / Deferred |
| **Notes** | Engineering notes, constraints, assumptions |

---

# 2. Requirements Traceability Matrix

| Req ID | Requirement Description | Type | Origin | WBS | Test Case(s) | DX-ID | Status | Notes |
|-------|--------------------------|------|--------|------|---------------|--------|--------|-------|

### ■ **Core Locking & Enforcement**

| Req ID | Requirement Description | Type | Origin | WBS | Test Case(s) | DX-ID | Status | Notes |
|--------|--------------------------|------|--------|------|---------------|--------|--------|-------|
| FR1 | User must be able to create soft/hard locks | FR | BRD §6 | 1.2 | T3, T4 | — | Implemented | UI complete Phase 1 |
| FR2 | Lock countdown must update in real time (±1s) | FR | PRD §2 | 1.2 | T3, T4 | — | Implemented | Uses DispatcherTimer |
| FR3 | System must terminate blocked apps ≤1s | FR | PoC | 1.3 | T6, T7 | — | Implemented | Verified Phase 1 |
| FR4 | System must re-kill apps if relaunched during lock | FR | PRD §2 | 1.3 | T15 | — | Implemented | Repeated monitor loop |

### ■ **Persistence & Reliability**

| Req ID | Requirement Description | Type | Origin | WBS | Test Case(s) | DX-ID | Status | Notes |
|--------|--------------------------|------|--------|------|---------------|--------|--------|-------|
| FR5 | Lock state must persist through crash or restart | FR | PRD §2 | 2.3 | T17 | **D2-01** | **Deferred** | Highest priority for Phase 2 |
| FR6 | App rules and settings must persist between sessions | FR | PRD §2 | 2.3 | T2 | — | Implemented | JSON OK; DB coming Phase 2 |
| NFR1 | CPU usage < 1% during idle/lock | NFR | PRD §3 | All | T16 | — | Verified | Meets performance target |

### ■ **Duration Selection & UX Behavior**

| Req ID | Requirement Description | Type | Origin | WBS | Test Case(s) | DX-ID | Status | Notes |
|--------|--------------------------|------|--------|------|---------------|--------|--------|-------|
| FR7 | Preset and custom durations must behave predictably and exclusively | FR | UX spec | 1.2 | T18 | **D2-02** | **Deferred** | Requires toggle and disable logic |
| FR8 | Lock restart while active must require confirmation | FR | UX spec | 1.2 | Manual | **D2-04** | **Deferred** | Prevent accidental resets |
| FR9 | App list changes during lock must follow defined UX rules | FR | UX spec | 1.2 | Manual | **D2-07** | **Deferred** | Needs UX decision: warn vs instant kill |

### ■ **Logging & Transparency**

| Req ID | Requirement Description | Type | Origin | WBS | Test Case(s) | DX-ID | Status | Notes |
|--------|--------------------------|------|--------|------|---------------|--------|--------|-------|
| FR10 | Activity feed must log all lock-related events | FR | PRD §2 | 1.2 | T10 | **D2-03** | **Deferred** | Cancel + expiration missing |
| FR11 | System must log termination events chronologically with timestamps | FR | PRD §2 | 1.3 | T10 | — | Implemented | Verified |

### ■ **Process & Application Mapping**

| Req ID | Requirement Description | Type | Origin | WBS | Test Case(s) | DX-ID | Status | Notes |
|--------|--------------------------|------|--------|------|---------------|--------|--------|-------|
| FR12 | Blocked apps must include dependent launcher processes | FR | PRD §2 | 2.4 | T6 | **D2-05** | **Deferred** | Riot → League hierarchy missing |
| FR13 | Pre-existing dependent processes must be terminated at lock start | FR | PRD §2 | 2.4 | T13, T14 | **D2-06** | **Deferred** | League not closing unless Riot selected |

### ■ **Installer, Branding, & Packaging**

| Req ID | Requirement Description | Type | Origin | WBS | Test Case(s) | DX-ID | Status | Notes |
|--------|--------------------------|------|--------|------|---------------|--------|--------|-------|
| NFR2 | MSIX must use branded identity (not GUID) | NFR | Packaging Spec | 1.4 | T11 | **D2-08** | **Deferred** | Branding update Phase 2 |
| NFR3 | Installer must install/uninstall cleanly | NFR | Packaging Spec | 1.4 | T11, T12 | — | Implemented | Verified manually |
| NFR4 | Installer must run without AV false positives | NFR | Security Spec | 1.4 | Manual | — | Pending | Requires certificate |

### ■ **Emergency Unlock & Safety**

| Req ID | Requirement Description | Type | Origin | WBS | Test Case(s) | DX-ID | Status | Notes |
|--------|--------------------------|------|--------|------|---------------|--------|--------|-------|
| FR14 | User must be able to emergency unlock with friction | FR | PRD §2 | 2.5 | Manual | — | Pending | Phase 2 work |
| FR15 | Emergency unlock must log a “relapse” event | FR | PRD §2 | 2.5 | Manual | — | Pending | Logging spec Phase 2 |

---

# 3. Summary of DX-ID Derived Requirements

| DX-ID | Derived Requirement | Requirement ID | Target Phase |
|-------|----------------------|----------------|--------------|
| D2-01 | Lock persistence | FR5 | Phase 2 |
| D2-02 | Predictable duration model | FR7 | Phase 2 |
| D2-03 | Complete activity logging | FR10 | Phase 2 |
| D2-04 | Confirmation on lock restart | FR8 | Phase 2 |
| D2-05 | Correct process hierarchy mapping | FR12 | Phase 2 |
| D2-06 | Correct pre-existing process termination | FR13 | Phase 2 |
| D2-07 | UX rules for mid-lock app toggling | FR9 | Phase 2 |
| D2-08 | Branding/identity update for MSIX | NFR2 | Phase 2 |

These requirements MUST appear in the Phase 2 WBS, MUST be implemented, and MUST be validated by test cases in the Phase 2 Final Verification Report.

---

# 4. Verification Requirements for Future Phases

- No requirement listed as **Deferred** may be marked **Implemented** or **Verified** until  
  corresponding test cases pass.
- All new DX-IDs generated in future phases (e.g., D3-01…) must be added to this RTM in a new section.
- Every requirement must map to both  
  a WBS task **and** at least one test case.

---

# 5. RTM Version History

| Version | Date | Changes |
|---------|-------|----------|
| 0.1 | 2025-11-02 | Initial RTM (minimal) |
| 1.0 | 2025-12-06 | Full enterprise RTM; DX-ID integration; expanded requirements list |

---

# End of Document
