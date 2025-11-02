# Product Requirements (PRD) — Cooldown.gg
*Version: 0.1 — 2025-11-02*

## 1. Overview
Windows desktop app that **removes the decision to play** during focus periods by enforcing locks and terminating targeted game processes.

## 2. Functional Requirements (MVP)
- **FR1 — Lock creation**: Soft/hard locks; presets (5/15/30/60/120/240) + custom minutes.
- **FR2 — Enforcement**: Detect & terminate targeted processes (parents + children) in <1s.
- **FR3 — Catalog**: Manage blocked apps (add/remove/toggle) with immediate effect.
- **FR4 — Persistence**: Windows Service (SYSTEM). Locks survive reboot; secure named pipes.
- **FR5 — Emergency unlock**: 60s friction timer + reason text → log locally.
- **FR6 — Stats**: Local counters (locks, terminations, minutes locked, emergency unlocks).
- **FR7 — Auth/Billing**: Login/register; Stripe Checkout & Portal; device limit = 3; 72h grace.
- **FR8 — Installer/Updates**: Signed MSIX; clean install/uninstall; auto-update plan.

## 3. Non-Functional Requirements
- **Performance**: Idle CPU <1%; detection <1s.
- **Reliability**: Service recovers; UI reconnects ≤2s after service restart.
- **Security**: DPAPI secrets; pipe ACL + per-boot nonce; least privilege.
- **Privacy**: No process names to cloud; only coarse counters.
- **Usability**: Onboarding <2 minutes; clear copy for hard-lock and emergency unlock.

## 4. Acceptance Criteria (condensed)
- Create/cancel soft locks; hard locks not cancelable; countdown accurate (±1s).
- After reboot during active lock, enforcement resumes automatically.
- Unauthorized pipe client rejected and logged; tamper events surface in UI.
- Stripe status change reflected client-side within 60s via webhook → cache update.
- Sentry captures unhandled exceptions; crash rate <5% in QA matrix.

## 5. Telemetry (local vs cloud)
- Local events: `lock_started`, `lock_canceled`, `lock_expired`, `process_terminated`, `emergency_unlock`.
- Cloud (coarse, optional): daily counts per device/user for locks/minutes/unlocks.

## 6. Dependencies
.NET 8, WPF, Windows Service, SQLite, FastAPI, PostgreSQL, Stripe, Sentry/Plausible.

## 7. Open Questions
- Final emergency unlock friction (copy & timer length) → validate in beta.
- Code signing vendor & cert timeline.
