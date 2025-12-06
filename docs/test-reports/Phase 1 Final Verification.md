# Phase 1 Final Verification – v0.2.1

**Date:** 2025-12-06  
**Tester:** C.J.  
**Build:** Cooldown.GG v0.2.1 (Release – Phase 1 Completion)  

---

## Scope
Validate full implementation of **Phase 1 (1.0 → 1.5)** deliverables:
Core Framework → Lock Manager → Process Monitor → Desktop UI → Installer → Pre-existing Process Termination.  
This test also identifies any non-critical issues to be deferred to Phase 2.

---

## Test Matrix

| ID | WBS Ref | Feature / Subsystem | Scenario | Expected Result | Actual Result | Pass/Fail |
|:--|:--|:--|:--|:--|:--|:--|
| T1 | 1.0 | Core Framework Setup | Clean clone → Build solution | Solution builds with 0 errors; shared core compiles under .NET 8 | Target framework .NET 8, Solution build 3 succeeded | Pass |
| T2 | 1.0 | Config Persistence | Edit blocked-apps list → Restart app | Changes persist across sessions (JSON config load/save verified) | Changes to 'Blocked Application' persist through app restart | Pass |
| T3 | 1.1 | Lock Manager – Soft Lock | Activate 15 min soft lock | Lock activates; cancel available; timer ticks 1 s intervals | Soft lock activates, cancel function works, UI shows toaster correctly | Pass |
| T4 | 1.1 | Lock Manager – Hard Lock | Activate hard lock | Cancel button disabled; UI shows remaining time | Hard lock activates, cancel function disabled, UI shows toaster | Pass |
| T5 | 1.1 | Event Propagation | Observe LockStateChanged event | ViewModels update in real time | UI correctly responsive across the board | Pass |
| T6 | 1.2 | Process Monitor – Detection | Start blocked app after lock start | App closes ≤ 1 s; Activity logs entry | Functional across the board, activity feed updated correctly, issue noted: Unless 'Platform' is blocked, applications from platform are not terminated/blocked | Partial Pass |
| T7 | 1.2 | Process Killer – Tree Kill | App launches child process | Parent and children terminated | Closes all associated applications at lock activation | Pass |
| T8 | 1.3 | Desktop Shell – UI Binding | Toggle lock states | LockStatusViewModel and Activity panel refresh immediately | Functional across the board | Pass |
| T9 | 1.3 | MVVM Commands | Use Start/Cancel buttons | Commands execute and reflect engine state | All input executes without issue | Pass |
| T10 | 1.3 | Activity Feed Persistence | Trigger multiple events | All appear chronologically with timestamps | UI responds correctly, activity feed works including chronological time stamping | Pass |
| T11 | 1.4 | Installer Build (MSIX) | Build → Install → Run → Uninstall | Installs cleanly; launches UI; uninstalls without residue | - | - |
| T12 | 1.4 | Distribution Files | Check config path and appdata folders | All files deployed correctly under %AppData%\Cooldown | 'blocker-config.json correctly located | Pass |
| T13 | 1.5 | Pre-existing Process Termination – Soft Lock | Steam running → Start soft lock | Steam terminates ≤ 1 s; toast “Closed 1 app” appears | Termination function (soft) works | Pass |
| T14 | 1.5 | Pre-existing Process Termination – Hard Lock | Riot Client running → Start hard lock | Riot terminates ≤ 1 s; no cancel; Activity entry added | Termination function (hard) works | Pass |
| T15 | 1.5 | Re-launch Prevention | Try to open Steam during lock | Process immediately killed; new log entry | - | Pass |
| T16 | All | Performance | Monitor CPU usage during active lock | Idle CPU < 1 % | CPU usage ~0.2–0.5% across idle, soft lock, hard lock; brief <1s spikes during kill events | Pass |
| T17 | All | Stability / Crash Recovery | Force-close desktop app → reopen | Lock state restores correctly from config | Lock does NOT persist; app loads with no lock active; processes are not blocked; state not restored. | Fail |
| T18 | 1.3 | Duration Selection Logic | Switch between preset durations and custom minutes | Selected preset applies unless custom chosen | All preset duration work, both UI and funtion. However, if a custom duration is set, there is no option revert to presets (see 'Known Issues'. | Partial Pass |

---

## Summary Results
**Total tests:** 18  
**Passed:**  
**Failed:**  
**Deferred to Phase 2:**  

---

## Environment
- **OS:** Windows 11 22H2  
- **Framework:** .NET 8.0-windows  
- **Build Configuration:** Release  
- **Hardware:** <CPU / RAM>

---

## Observations & Notes
# Issues reported that are NOT failures but must be logged for Phase 2
These are not part of Phase-1 functional requirements but are improvements, UX refinements, or tighter behavior control for Phase-2.

A. Missing Activity Log Entries (Phase-2 Logging Enhancements)
1. No Activity message when a soft lock is canceled
→ Should appear: “Soft lock canceled.”
2. No message when a lock expires
→ Should appear: “Lock expired at HH:MM.”

B. Lock Interaction & UX Issues
3. Starting a new lock while one is active immediately restarts the lock
→ Should show confirmation dialog:
“A lock is already active. Start a new one?”

4. Duration selection ambiguity
- Custom input overrides presets silently
- Needs a “Use custom duration” toggle
- Presets should auto-disable when custom is active

C. Blocked Applications Logic Issues
These were discovered organically through use, not through formal tests — but extremely important:
5. League of Legends is not blocked unless RiotClientServices is checked
- This means nested launcher processes are not fully resolved
- Phase-2 should include multi-process block mapping
6. At lock creation, League is NOT terminated if only League is checked
- This is a misalignment with pre-existing process termination logic
7. If user checks a blocked application while a lock is already active → the app is instantly killed
- This is technically correct but may need UX adjustment or confirmation

# Behavioral Notes (Not necessarily bugs, but must be remembered)
- Lack of “Lock Expired” message (noted)
- Lack of “Soft Lock Canceled” message (noted)
- Lack of confirmation when creating a lock while one is active (noted)
- Riot/League process mapping inconsistencies (noted)
- The immediate kill when checking a new app during lock (noted — may be intended or needs refinement)

These are all earmarked for Phase 2: UX + Logic Hardening.

# Structural insight during testing
Key architectural observations:
- LockManager persists some config but not lock state
- Bootstrap initialization does not rehydrate lock state
- Process killer works well during active lock, but pre-existing process termination is inconsistent depending on process hierarchy (Riot/League case)
- CPU efficiency is excellent — foundation is strong
- Blocked apps logic and event propagation are very good except for the few UX items you spotted
- MSIX package identity name is still GUID-based (will be updated to branded identity in Phase 2)

# Summary
Cooldown Phase-1 is nearly complete, with strong performance and core locking functionality, but missing the critical feature of lock persistence across crashes, and needs several Phase-2 behavioral and UX refinements that you identified.

- Verified `App.xaml.cs` fix (`App : System.Windows.Application`) resolves build errors.  
- Termination at T0 confirmed within ≤1 s for Steam and Riot.  
- Installer/MSIX build installs and runs successfully.  
- Activity logging and UI updates operate per spec.  
- CPU usage idle < 1 %.  
- Duration-selection behavior requires refinement (see 'Known Issues').  

---
## Deferred to Phase 2

### **D2-01 — Missing Lock State Persistence and Crash Recovery**
**Category:** Logic / Architecture  
**Description:**  
Active locks do not persist across application crashes or forced closure. On restart, Cooldown loads in an unlocked state and no longer blocks applications.  
**Impact:** High — Core functional gap.  
**Reason for Deferral:**  
Requires implementation of full LockManager state serialization and bootstrap rehydration. Too large to patch mid-phase; belongs in Phase 2’s logic hardening stage.  
**Target Phase:** Phase 2  
**Dependencies:** Config persistence rewrite, startup initialization routines.

---

### **D2-02 — Preset vs Custom Duration Ambiguity**
**Category:** UX / UI Logic  
**Description:**  
Custom duration silently overrides preset durations without an explicit toggle. No clear way to revert to presets once custom is set.  
**Impact:** Medium — Causes user uncertainty.  
**Reason for Deferral:**  
Requires additional UI controls (toggle or radio group) and conditional logic.  
**Target Phase:** Phase 2  
**Dependencies:** Lock duration selection redesign.

---

### **D2-03 — Missing Activity Messages for Cancellation and Expiration**
**Category:** UX / Logging  
**Description:**  
No Activity entries are logged when a soft lock is canceled or when any lock (soft/hard) expires.  
**Impact:** Low–Medium — Reduces transparency and debugging clarity.  
**Reason for Deferral:**  
Not blocking functionality; belongs in Phase 2 where logging system is expanded.  
**Target Phase:** Phase 2  
**Dependencies:** ActivityFeed service updates.

---

### **D2-04 — Lock Restart Occurs Without Confirmation**
**Category:** UX  
**Description:**  
If a user starts a new lock while one is active, Cooldown restarts the lock immediately without prompting.  
**Impact:** Medium — Can cause accidental lock resets.  
**Reason for Deferral:**  
Requires design of a confirmation dialog and cross-component event handling.  
**Target Phase:** Phase 2  
**Dependencies:** Dialog service, LockManager call guards.

---

### **D2-05 — Riot/League Process Mapping Inconsistency**
**Category:** Process Detection / Logic  
**Description:**  
League of Legends is not blocked unless RiotClientServices is manually selected. This breaks expected behavior where selecting the game should implicitly include its launcher.  
**Impact:** Medium — Allows bypass and weakens reliability.  
**Reason for Deferral:**  
Requires multi-process dependency mapping and hierarchical block list logic.  
**Target Phase:** Phase 2  
**Dependencies:** Process tree detection improvements.

---

### **D2-06 — Pre-existing League Client Not Terminated If Only “League” Is Selected**
**Category:** Process Termination  
**Description:**  
At lock activation, League is not terminated if only “League of Legends” is checked and Riot services are not.  
**Impact:** Medium — Breaks lock completeness and consistency.  
**Reason for Deferral:**  
Requires improved process lineage detection and rule-based block propagation.  
**Target Phase:** Phase 2  
**Dependencies:** Process monitor rewrite (hierarchical).

---

### **D2-07 — Adding a Blocked Application During Active Lock Instantly Terminates It**
**Category:** UX / Behavior  
**Description:**  
If a user checks a new blocked application while a lock is already active, Cooldown immediately terminates the app without warning.  
**Impact:** Low–Medium — Behavior is correct logically but may be too aggressive.  
**Reason for Deferral:**  
Requires UX decision: warning popup vs keeping instantaneous logic.  
**Target Phase:** Phase 2  
**Dependencies:** UI–LockManager communication pattern.

---

### **D2-08 — MSIX Package Identity Still GUID-Based**
**Category:** Packaging / Branding  
**Description:**  
The MSIX Identity uses a GUID instead of a branded name. Task Manager and Windows UI show a non-descriptive identifier.  
**Impact:** Low — Cosmetic but affects professionalism and user trust.  
**Reason for Deferral:**  
Branding overhaul belongs in Phase 2’s distribution and packaging work.  
**Target Phase:** Phase 2  
**Dependencies:** Package.appxmanifest update, certificate signing.

---

**Signature:** __________________  **Date:** _________
