# Requirements Traceability Matrix (RTM) — Cooldown.gg
*Version: 0.1 — 2025-11-02*

| Business Goal | Requirement ID | WBS Task(s) | Test Case / Acceptance | Metric |
| --- | --- | --- | --- | --- |
| Reduce impulsive gaming | R1 / FR1 Lock creation | Phase 1.2 Dashboard | Create soft/hard lock; countdown ±1s | Activation ≥50% |
| Enforce blocks reliably | R2 / FR2 Enforcement | Phase 0 PoC; Phase 2.4 | Detect & kill <1s; child processes exit | Avg locks/week ≥10 |
| Survive restarts | R3 / FR4 Persistence | Phase 2.1–2.3 | Reboot resumes lock; UI reconnect ≤2s | Crash rate <5% |
| Safety valve | R4 / FR5 Emergency unlock | Phase 2.5 | 60s timer; reason captured; event logged | Emergency unlocks trend ↓ |
| Insight & value | R5 / FR6 Stats | Phase 4.1 | Local counters visible | Trial → Paid ≥20–30% |
| Monetize | R6 / FR7 Auth/Billing | Phase 3.1–3.6 | Stripe status reflected ≤60s | Churn <5% |
| Privacy-first | R7 / PRD §5 | Phase 4.6 + App E | No process names in cloud | NPS > 40 |

*IDs reference: BRD §6 (R-series) and PRD §2 (FR-series).*
*** End Patch
