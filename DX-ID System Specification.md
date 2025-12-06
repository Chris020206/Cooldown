# DX-ID SYSTEM — Deferred Issue Identification Standard  
*(For Cooldown.GG Development & QA Pipeline)*

Version: 1.0  
Author: ChatGPT  
Date: 2025-12-06  

---

## 1. Purpose  
The DX-ID System is the standardized method for identifying, numbering, tracking, and propagating deferred issues across all development phases of Cooldown.GG.  
It ensures full traceability between QA reports, WBS, RTM, CHANGELOG, and GitHub issues.

This document defines:

- Naming conventions  
- Numbering rules  
- Propagation logic across phases  
- Status stages  
- AI interpretation rules  
- Cross-document integration  

---

## 2. DX-ID Format

Each deferred issue uses the universal identifier:

```
D[PHASE]-[ISSUE NUMBER]
```

Where:

- **D** = Deferred  
- **[PHASE]** = The phase where the issue originated  
- **[ISSUE NUMBER]** = Sequential index (01, 02, 03…)

Examples:

- `D2-01` → First issue found in Phase 1, deferred to Phase 2  
- `D3-02` → Second issue found in Phase 2, deferred to Phase 3  

DX-IDs NEVER change once assigned.

---

## 3. Numbering Rules

### **Rule 1 — Origin Phase Defines the Number**
If an issue is discovered in Phase 1 and deferred:

`D2-xx`

If discovered in Phase 2 and deferred:

`D3-xx`

The PHASE always represents **origin**, not destination.

---

### **Rule 2 — Sequential Numbering (No Renumbering)**
Issues are numbered in the order they are confirmed as deferred.

Example sequence:

- `D2-01`
- `D2-02`
- `D2-03`
- ...

Numbers are **never** changed, removed, or reused.

---

### **Rule 3 — Permanent Identifiers**
DX-IDs remain constant across:

- Test reports  
- WBS  
- RTM  
- CHANGELOG  
- GitHub  
- Future phases  

If an issue evolves, use:

```
D3-12 (supersedes D2-01)
```

Never renumber or delete.

---

### **Rule 4 — Required Metadata**
Each DX-ID must reference:

- WBS Reference  
- Feature / Component  
- Category (Logic / UI / UX / Architecture / Etc.)  
- Impact Level  
- Reason for Deferral  
- Target Phase  
- Dependencies (if any)

This metadata ensures enterprise-level traceability.

---

### **Rule 5 — Cross-Document Propagation**
Every DX-ID appears in:

1. **Test Report** → Under “Deferred to Phase X+1”  
2. **WBS** → As a requirement/task for next phase  
3. **RTM** → As a required capability or refinement  
4. **CHANGELOG** → Marked as “Resolved D2-XX” when completed  
5. **GitHub Issues** → As an issue or milestone item  

This guarantees no issue is ever lost.

---

## 4. Multi-Phase Propagation Rules

### If a deferred issue is NOT completed in the next phase:
DO NOT create a new ID.

Example:

Original:
`D2-05` → Deferred into Phase 2

If unresolved:
`D2-05` continues into Phase 3

The ID always reflects **where the issue originated**, not where it is completed.

---

## 5. DX-ID Status Stages

Each deferred issue moves through the following lifecycle:

### **1. Deferred**  
Automatically assigned after Phase testing identifies it.

### **2. In Progress**  
Developer assigned; work underway.

### **3. Resolved**  
Implemented in a development build.

### **4. Verified Fixed**  
Confirmed resolved by QA in the next verification phase.

Example CHANGELOG entry:

```
Resolved D2-03 — Added Activity Feed entries for Lock Cancellation and Expiration
Verified fixed in Phase 2 Final Verification
```

---

## 6. AI Interpretation Rules  
*(Defines how ChatGPT should track and interpret DX-IDs internally)*

1. When ChatGPT sees `D2-05`, it understands:
   - Issue originated in Phase 1  
   - Intended for Phase 2  
   - Must persist until marked resolved  

2. If unresolved during Phase 2:
   - It continues to Phase 3  
   - ID remains `D2-05`  

3. When generating next-phase test plans:
   - All unresolved DX-IDs become mandatory test cases  

4. When generating next-phase WBS:
   - DX-IDs automatically become tasks  

5. When building CHANGELOG:
   - A “Resolved D2-XX” entry is created  
   - Then marked “Verified Fixed” in the next test report  

This ensures perfect continuity across all documents.

---

## 7. Example DX-ID Lifecycle

### Phase 1 Test Report
Lock does not persist after crash → assigned `D2-01`.

### Phase 2 WBS
```
2.1 Implement Lock State Persistence (Resolves D2-01)
```

### Phase 2 RTM
Requirement R2.3 → “System must restore lock state after crash (D2-01)”

### Phase 2 CHANGELOG
```
Resolved D2-01 — Lock state now persists across crashes
```

### Phase 2 Final Verification
```
D2-01 — Verified fixed
```

This completes the lifecycle.

---

## 8. Summary

The DX-ID system:

- Prevents issue loss  
- Maintains cross-phase traceability  
- Supports professional QA workflows  
- Enables AI-driven continuity  
- Ensures stability in long-term development  
- Creates an audit-friendly documentation chain  

All future phases will use this structure.

---

**End of Document**  
Version 1.0
