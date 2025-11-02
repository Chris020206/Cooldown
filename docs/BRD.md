# Business Requirements Document (BRD) — Cooldown.gg
*Version: 0.1 — 2025-11-02*

## 1. Problem Statement
Impulsive gaming consumes time intended for study/work. Existing blockers are generic and easy to ignore; gamers want a **friction-enforced way** to remove the moment-of-weakness decision.

## 2. Objectives & KPIs
- Reduce impulsive sessions: **Avg locks/user/week ≥ 10**.
- Activation: **≥ 50%** of new installs create a first lock in week 1.
- Trial → Paid: **≥ 20–30%**; Monthly churn **< 5%**.
- Reliability: block detection **< 1s**, crash rate **< 3–5%**.

## 3. Scope (MVP)
- Windows-only WPF app; soft/hard timed locks; block common game launchers and custom processes; lock persists through reboot; basic local stats; Stripe subscription.

## 4. Out of Scope (MVP)
Earned credits, streaks, recurring schedules, mobile app, parental mode, community features.

## 5. Personas
- **Student** (primary): Needs enforced focus blocks during study windows.
- **Recovering gamer**: Wants hard locks with emergency unlock & logging.
- **Parent** (secondary, post-MVP): Guardian controls and reports.

## 6. High-Level Requirements
- **R1**: Create soft/hard locks with presets & custom minutes.
- **R2**: Maintain blocked app catalog; enforce within <1s.
- **R3**: Service persistence + secure IPC; survive reboot.
- **R4**: Friction-based emergency unlock with reason capture.
- **R5**: Local stats (locks, blocks, minutes saved).
- **R6**: Auth + billing + offline grace (72h).
- **R7**: Privacy-first telemetry (coarse events only).

## 7. Assumptions & Constraints
Windows 10 1809+; .NET 8; admin for install; DPAPI for secrets; no cloud storage of process names.

## 8. Risks (Top)
AV false positives; Windows updates breaking service; user friction around elevation; bypass via Safe Mode (educate/log stance).

## 9. Success Criteria
KPIs in §2 met in first 60–90 days of beta; positive qualitative feedback from ≥15 beta users.
*** End Patch
