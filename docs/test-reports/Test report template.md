# Phase [X] Final Verification — v[VERSION]

**Date:** [YYYY-MM-DD]  
**Tester:** [Name / Initials]  
**Build:** [Build Identifier]  

---

## 1. Scope
Briefly define what Phase [X] is responsible for delivering.  
State that this test validates the implementation of those deliverables and identifies items deferred to Phase [X+1].

---

## 2. Test Matrix

| ID | WBS Ref | Feature / Subsystem | Scenario | Expected Result | Actual Result | Pass/Fail |
|:--|:--------|:---------------------|:---------|:----------------|:--------------|:----------|
| T1 | [ref] | [feature] | [scenario] | [expected] | [actual] | [result] |
| T2 | ... | ... | ... | ... | ... | ... |
| ... | ... | ... | ... | ... | ... | ... |

*(All tests for the phase go here. T17 should always be crash recovery.)*

---

## 3. Summary Results

**Total tests:** [N]  
**Passed:** [N]  
**Failed:** [N]  
**Deferred to Phase [X+1]:** [Count of D-items]  

---

## 4. Environment

- **OS:**  
- **Framework:**  
- **Build Configuration:**  
- **Hardware:**  

*(This ensures reproducibility.)*

---

## 5. Observations & Notes  
*(Dense exploratory section — everything that may or may not matter goes here.)*

Use this area for:

- Detailed behavioral notes  
- UX impressions  
- Anomalies  
- Edge-case findings  
- Hypotheses  
- Design considerations  
- Secondary observations  
- Performance spikes or anomalies  
- Logging outputs  
- All the “throw everything at the wall” material  

**Important:**  
This section is *not* authoritative.  
Everything here will be processed and distilled into formal deferred items in Section 6.

---

## 6. Deferred to Phase [X+1]  
*(This is the formal, authoritative, distilled list.)*

Each item follows the **DX-ID standard**:

### **DX-01 — [Title]**  
**Category:** [Logic / UI / UX / Performance / Architecture / Security / Packaging]  
**Description:**  
[Clear, concise explanation of the issue.]  
**Impact:** [High / Medium / Low]  
**Reason for Deferral:**  
[Why this was not fixed in Phase X.]  
**Target Phase:** Phase [X+1]  
**Dependencies:**  
[List if any; otherwise "None".]

### **DX-02 — [...]**  
...

*(Repeat for each deferred item.)*

---

## 7. Certification
A formal signature section.

**Signature:** __________________  
**Date:** __________________  

