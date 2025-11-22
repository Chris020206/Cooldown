# Cooldown.gg Work Breakdown Structure (WBS)

*Version: 1.1 — Last updated November 2, 2025*

> **WBS Addendum (2025-11-02)**  
> This document now includes:  
> (1) phase-level **acceptance criteria**,  
> (2) a **security & tamper model** summary,  
> (3) **privacy/GDPR** notes,  
> (4) a **telemetry plan** tied to success metrics,  
> (5) **release/rollback** guidance,  
> (6) a compact **QA test matrix**, and  
> (7) added **risks** & **marketing validation** activities.  
> These improve execution clarity and demonstrate Business Analyst rigor.

The following WBS captures the phased development plan for turning the Cooldown.gg proof of concept into a production-ready desktop experience.

## 🎯 MVP Definition

**Core Value Proposition**

A Windows desktop application that blocks games and distracting apps during user-defined locks to help gamers control impulsive play and build healthy habits.

**Strategic Positioning**

- **Primary Focus:** Gaming addiction solution
- **Technical Capability:** Block any application (games, launchers, social media, browsers, custom apps)
- **Marketing:** Gaming-specific messaging, community, and features
- **Differentiation:** Earned credits, streaks, gaming-specific UI—features competitors do not have today

**MVP Success Criteria**

- ✅ User can install the app
- ✅ User can select apps to block (gaming plus custom apps)
- ✅ User can create timed locks (soft and hard) with confirmation dialogs
- ✅ Apps are reliably blocked during locks
- ✅ User can see lock status
- ✅ Locks persist through app and system restarts
- ✅ User can subscribe ($7–10/month) and manage billing
- ✅ Basic usage stats (time saved, locks created)

**Explicitly Out of Scope for MVP**

- ❌ Earned credits system (manual task checklist) → Post-MVP priority: high
- ❌ Streaks and achievements → Post-MVP priority: high
- ❌ Weekly email summaries → Post-MVP priority: medium
- ❌ Mobile companion app → Post-MVP priority: low
- ❌ Scheduled recurring locks → Post-MVP priority: high
- ❌ Parental controls → Post-MVP priority: medium
- ❌ Health/fitness integrations → Post-MVP priority: low
- ❌ Community features → Post-MVP priority: low

## 📊 Development Phases Overview

| Phase | Goal | Deliverable | Status |
| --- | --- | --- | --- |
| Phase 0 | PoC validation | Working console blocker | ✅ Complete |
| Phase 1 | Core desktop app | Installable app with UI | 🔄 Next |
| Phase 2 | Windows service + persistence | Production-grade blocking | ⏳ Pending |
| Phase 3 | Backend + auth + billing | User accounts & subscriptions | ⏳ Pending |
| Phase 4 | MVP polish | Beta-ready product | ⏳ Pending |
| Phase 5 | Post-MVP features | Credits, streaks, analytics | ⏳ Future |

Timeline: flexible (student schedule).

Approach: work in bursts between classes and exams with no rigid deadlines.

Estimated effort: 4–8 months at student pace, completing phases as time allows.

## Phase 0: Proof of Concept ✅ Complete

**Deliverables**

- ✅ Console application with process blocking
- ✅ Soft lock (cancelable)
- ✅ Hard lock (non-cancelable)
- ✅ Multi-app support (Steam, Riot Client, any process)
- ✅ Real-time process monitoring (<1-second detection)
- ✅ Child process termination

**Test Results**

- ✅ Steam blocked instantly (no visual window)
- ✅ Riot Client blocked instantly
- ✅ Lock cancellation working (soft locks only)
- ✅ Hard locks cannot be canceled (enforced)
- ✅ Apps open normally after lock expiration
- ✅ 100% blocking success rate during testing

**Status**

Validated on November 1, 2025—core blocking mechanism proven feasible and reliable.

## Phase 1: Desktop Application Foundation

Goal: Create installable desktop app with gaming-focused UI.

Timeline: flexible student schedule.

### 1.1 UI Framework Setup (2–3 days)

- Create WPF project (.NET 8)
- Set up MVVM architecture (view models, views, models)
- Design system tray icon integration
- Implement basic navigation structure
- Choose UI component library (MaterialDesignInXAML or ModernWPF)
- Establish gaming aesthetic (dark theme, purple/blue accents)

**Deliverables**: working WPF app shell, system tray integration, base layout with gaming visual identity.

### 1.2 Core UI Screens (5–7 days)

#### Lock Control Dashboard

- Lock creation buttons (soft/hard) with preset durations
- Confirmation dialogs (especially for hard locks)
- Active lock display with countdown and end time
- Cancel option (disabled for hard locks)
- Visual status indicators and block attempt notifications

#### App Selection Screen

- Auto-detected categories (gaming launchers, popular games, social, entertainment, browsers)
- Manual app addition (file picker, process detection, icon extraction, categorization)
- App management (enable/disable toggles, removal, search/filter)
- Gaming-first UX with appropriate terminology

#### Settings Screen

- App preferences (start with Windows, minimize to tray, notifications)
- Lock preferences (default durations, confirmation settings)
- Account section (subscription status, billing link, device management)
- About section (version info, policy links)

#### Stats Dashboard (MVP scope)

- Today/weekly/lifetime stats (locks created, blocks, estimated time saved)

**Deliverables**: functional UI covering core workflows with a gaming aesthetic and language.

### 1.3 UI ↔ PoC Integration (3–4 days)

- Extract blocking engine into shared library
- Build in-process communication layer
- Wire UI interactions to engine
- Surface real-time lock status and notifications in UI
- Handle engine failures gracefully

**Deliverables**: UI controls the engine, real-time updates flow back, end-to-end path validated.

### 1.4 Installer & Distribution (2–3 days)

- Package with MSIX (Windows 10/11)
- Request admin elevation during install
- Configure start-on-boot (user-configurable)
- Validate on clean Windows VM
- Add auto-update mechanism (Squirrel.Windows or MSIX)
- Plan for code signing certificate (optional for MVP, required for production)

**Deliverables**: one-click installer, optional auto-start, update notifications, antivirus-friendly package.

**Phase 1 Success Criteria**

- User can install and use the UI without the console
- Users can create locks via UI and manage blocked apps
- Blocking remains reliable with system tray integration
- Confirmation dialogs prevent accidental hard locks

**Phase 1 Acceptance Criteria**

- WPF app launches on Win10 22H2 and Win11 23H2+; tray icon accessible; start minimized works.
- Create soft/hard locks from UI; real-time countdown updates ≤1s jitter.
- Blocked app list: add/remove/toggle; immediate effect without restart.
- CPU idle < 1%; working set < 100 MB while idle; no unhandled exceptions in session logs.
- MSIX build artifact produced; install/uninstall succeeds on clean VM.

## Phase 2: Windows Service & Persistence

Goal: production-grade blocking that survives restarts and resists tampering.

### 2.1 Windows Service Development (4–5 days)

- Build .NET 8 Windows service
- Implement lifecycle methods and recovery
- Run as SYSTEM with elevated privileges
- Provide install/uninstall automation and logging

### 2.2 Inter-Process Communication (3–4 days)

- Named pipes server (service) and client (UI)
- Define JSON command protocol with authentication
- Ensure resiliency (auto-reconnect, heartbeats)

### 2.3 State Persistence (3–4 days)

- SQLite in ProgramData with migrations
- Persist lock state, app rules, settings, and events
- Restore active locks on restart and sync configuration

### 2.4 Enhanced Blocking (optional, 2–3 days)

- Firewall, hosts file, or registry-based blocking if PoC approach is insufficient

### 2.5 Tamper Resistance & Emergency Unlock (3–4 days)

- Service watchdog, config encryption, integrity checks
- Friction-based emergency unlock (cool-down timer, reflection prompts)
- Log bypass attempts as relapse events

**Phase 2 Success Criteria**

- Service runs on startup and persists locks through reboots
- Secure IPC between UI and service
- Harder to bypass while keeping emergency unlock safety valve

**Phase 2 Acceptance Criteria**
- Service (SYSTEM) starts automatically; UI reconnects via named pipes within 2s after service restart.
- Active lock survives OS reboot; expiry is honored if OS time changed forward/back by ≤5 minutes.
- IPC restricts client to current user session; requests include per-boot nonce; unauthorized client is rejected and logged.
- Config stored under `%ProgramData%\CooldownGG`; SQLite schema versioned; migration script tested.
- Emergency unlock requires ≥60s friction timer + reason entry; event logged locally as “relapse”.
- Basic tamper checks: missing/broken service → UI banner + repair action; bypass attempts logged.

## Phase 3: Backend API & Authentication

Goal: enable accounts, authentication, and subscription management.

### 3.1 Backend Infrastructure Setup (2–3 days)

- FastAPI deployment with managed PostgreSQL (e.g., Railway)
- CI/CD pipeline and environment configuration

### 3.2 Database Schema Implementation (2–3 days)

- Users, devices, subscriptions, and audit log tables via migrations

### 3.3 Authentication System (3–4 days)

- JWT-based auth, refresh tokens, password reset, device limits

### 3.4 Desktop App Authentication (2–3 days)

- Login/register screens, secure token storage, offline grace period

### 3.5 Stripe Integration (4–5 days)

- Checkout sessions, webhooks, billing portal, entitlement updates

### 3.6 Entitlement System (2 days)

- Enforce subscription status with cached offline grace period and feature gating

**Phase 3 Success Criteria**

- Users can register, subscribe, and manage billing
- Desktop app respects subscription status and device limits
- Offline mode functions for 72 hours

**Phase 3 Acceptance Criteria**

- FastAPI + Postgres deploy with CI/CD; migrations idempotent.
- JWT + refresh flow; tokens stored with DPAPI; device cap=3 enforced.
- Stripe Checkout + webhooks: subscription_created/updated/canceled reflected within 60s.
- Entitlements cached locally; offline grace up to 72h; beyond that, lock creation is disabled and UI explains why.
- PII minimization: email, hashed device id, coarse events only (no process names in cloud).

## Phase 4: MVP Polish & Beta Launch

Goal: deliver a production-quality experience.

### 4.1 Basic Analytics & Stats (2–3 days)

- Track locks, blocks, emergency unlocks, and time saved locally (optional cloud sync)

### 4.2 Onboarding & First-Time UX (2–3 days)

- Guided setup wizard, value-focused copy, positive reinforcement

### 4.3 Error Handling & Stability (3–4 days)

- Comprehensive error handling, crash reporting, graceful degradation

### 4.4 Performance Optimization (2 days)

- CPU <1%, memory <100 MB, fast detection, responsive UI

### 4.5 Testing & QA (3–4 days)

- Manual end-to-end QA, edge case testing, bug triage, issue documentation

### 4.6 Legal & Compliance (1–2 days)

- Privacy policy, terms of service, refund policy, GDPR considerations

### 4.7 Landing Page & Marketing Site (3–4 days)

- Gaming-focused marketing site with SEO, Stripe checkout links, analytics

**Phase 4 Success Criteria**

- Polished app with smooth onboarding and minimal bugs
- Legal foundations established
- Marketing site ready for beta sign-ups

**Phase 4 Acceptance Criteria**
- Onboarding wizard completes <2 minutes; first lock created successfully by ≥95% test users.
- Crash rate <5% across QA matrix (see Appendix F); Sentry capturing unhandled exceptions.
- Privacy Policy, Terms, Refund Policy published; in-app links reachable.
- Landing page collects ≥25 qualified beta signups; UTM tracked; Plausible configured.

## Phase 5: Post-MVP Features (Backlog)

Prioritized based on user feedback after MVP launch.

1. **Earned Credits System (High priority, 1–2 weeks)**
   - Task checklists, credit balance, gated launches, history view
2. **Streaks & Achievements (High, 1 week)**
   - Streak tracking, badges, celebratory notifications
3. **Scheduled Recurring Locks (High, 1 week)**
   - Day/time schedules, pre-lock warnings, overrides with friction
4. **Weekly Email Summaries (Medium, 3–4 days)**
   - Automated reports with time saved and streak updates
5. **Relapse Handling & Support (Medium, 1 week)**
   - Trigger logging, reflection prompts, suggested actions
6. **Browser Extension (Medium, 1–2 weeks)**
   - Block gaming-related sites during locks, sync with desktop
7. **Parental Mode (Medium, 1–2 weeks)**
   - Guardian approvals, usage reporting, stricter enforcement
8. **Mobile Companion App (Low, 3–4 weeks)**
   - Remote lock control, push notifications, stats dashboard
9. **Health/Fitness Integrations (Low, 2–3 weeks)**
   - Earn credits via fitness trackers, manual overrides
10. **Community Features (Low, 3–4 weeks)**
    - Accountability partners, leaderboards, social sharing

## Phase 6: Multi-Brand Strategy (Long-Term)

Evaluate launching a productivity-focused sister brand once Cooldown.gg surpasses 500 paying users and demonstrates demand for non-gaming app blocking.

- Assess adoption metrics and market appetite
- Reuse shared codebase with rebranded UI and messaging
- Explore pricing and bundling strategies

## 🛠️ Development Resources & Team

- Solo founder with AI assistance
- Optional contract designer for UI polish
- Beta testers from gaming addiction communities

**Tools & Infrastructure**

- Development: Visual Studio 2022, VS Code, GitHub
- Backend & hosting: FastAPI, PostgreSQL (managed), optional Redis, Railway
- Email: SendGrid or Mailgun (free tier)
- Payments: Stripe Checkout + Customer Portal
- Monitoring: Sentry, Plausible analytics (optional PostHog later)

## 📅 Timeline Estimates

Flexible student schedule with bursts of work between academic commitments.

- **Phase 1 completion:** 4–6 weeks (target January 2026)
- **Phase 2 completion:** 4–6 weeks (target March 2026)
- **Phase 3 completion:** 3–4 weeks (target May–June 2026)
- **Phase 4 completion:** 2–3 weeks (target July–August 2026)
- **Public beta launch:** Target August 2026

Expect some weeks with 20 hours of progress and others with zero; consistency matters more than speed.

## 🎯 Success Metrics (Post-Launch)

- **Beta phase:** 50–100 beta users, <5% crash rate, 50% activation, 20% trial-to-paid, qualitative feedback
- **Early growth:** 500–1,000 paying users, 30% trial-to-paid, <3% monthly churn, average 10+ locks/week/user, $3.5K–$10K MRR
- **Scaling:** 2,000–5,000 paying users, $14K–$50K MRR, NPS > 40, <5% support ticket rate, organic community recognition
- **Long-term:** 10,000+ paying users, $70K–$100K+ MRR, sustainable solo-founder business

## 🚨 Risk Mitigation

| Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- |
| Users bypass via Safe Mode | Medium | High | Detect & log, educate users, avoid cat-and-mouse battles |
| Stripe webhook failures | Low | High | Retry queues, monitoring, manual reconciliation |
| Windows updates break service | Medium | Medium | Test on Insider builds, keep automated regression tests, ship hotfixes |
| Competitor launches gaming blocker | Low | Medium | Build community moat, emphasize credits differentiation |
| Low conversion rate (<20%) | Medium | High | Strong onboarding, free trial, iterate messaging |
| Hard to explain value to non-addicted gamers | Medium | Medium | Focus messaging on severe cases and r/StopGaming |
| Solo founder burnout | Medium | High | Flexible timeline, celebrate wins, maintain sustainability |
| Technical scope creep | Low | Medium | Keep MVP minimal, defer features, leverage validated PoC |
| AV/EDR false positives on process kill | Medium | Medium | Code signing, allow-listing docs, signed MSIX. |
| Installer elevation friction | Medium | Low | Clear rationale in installer/UI; signed package. |
| Stripe regional/payment edge cases | Low | Medium | Use Stripe Portal; generous grace period; manual override path. |
| Community pushback on “hard locks” | Low | Medium | Emphasize emergency unlock; informed consent during onboarding. |

## 📋 Decision Log

**Confirmed on November 1, 2025**

1. Gaming-focused positioning → ✅ Approved
2. Block all apps but market as gaming-first → ✅ Approved
3. Defer earned credits to post-MVP → ✅ Approved
4. Defer streaks to post-MVP → ✅ Approved
5. Windows-only MVP → ✅ Approved
6. Emergency unlock with friction → ✅ Approved
7. Flexible student timeline → ✅ Approved
8. Solo founder + AI development → ✅ Approved
9. $7–10/month premium pricing → ✅ Approved
10. Multi-brand strategy deferred → ✅ Approved

**Decisions pending**

- UI framework (WPF recommended) → decide before Phase 1
- Backend hosting (Railway recommended) → decide before Phase 3
- Code signing certificate (recommended before launch)
- Analytics tooling (start simple; evaluate PostHog later)
- Final emergency unlock friction → validate during beta

## 📝 Notes & Assumptions

- Windows 10 version 1809+ only
- English language and USD pricing for MVP
- Email-based support, self-serve billing via Stripe portal
- No enterprise, white-label, or third-party APIs at launch

**Out of scope**

- macOS or Linux versions
- Enterprise or school licenses
- Hardware integrations
- General-purpose API platform

**Technical constraints**

- Requires .NET 8 runtime and admin privileges
- Needs constant internet for subscription checks (72-hour offline grace)
- Limit of three devices per account

## ✅ Next Steps

**Immediate (this week)**

- Review and approve WBS
- Set up development environment & repo if not already done
- Create project management board
- Finalize UI framework decision (recommend WPF)

**Short-Term (next 2–4 weeks)**

- Start Phase 1.1 (UI framework setup)
- Produce UI mockups and brand assets
- Secure domain (cooldown.gg) if available

**Medium-Term (next 1–3 months)**

- Complete Phase 1 desktop UI
- Begin Phase 2 service work
- Recruit beta testers and start landing page

**Long-Term (next 4–8 months)**

- Finish Phases 2–4
- Launch public beta
- Iterate based on user feedback

## 🎉 Closing Thoughts

Achievements as of November 1, 2025:

- ✅ Working PoC built and validated in one day
- ✅ Core blocking technology proven with 100% success in testing
- ✅ Product strategy and roadmap established with clear differentiation
- ✅ Decisions made to stay focused on gaming audience

**Guiding principles**

- Progress over perfection—ship iteratively
- User problems over feature checklists
- Sustainable pace over burnout
- Authentic storytelling over corporate tone

You have a roadmap, community, and tooling lined up—time to build Cooldown.gg. 🎮🚀

## 📎 Appendices

### Appendix A: Tech Stack Summary

- **Desktop application:** C# (.NET 8), WPF (recommended), MVVM, SQLite, named pipes, MSIX installer
- **Windows service:** C# (.NET 8), SYSTEM privileges, named pipes, SQLite + registry
- **Backend API:** Python FastAPI, PostgreSQL, optional Redis, Railway hosting, JWT auth
- **Payments:** Stripe Checkout, Customer Portal, webhooks
- **Marketing site:** Next.js or Astro, Vercel/Netlify hosting, Plausible analytics

### Appendix B: Learning Resources

- WPF documentation and tutorials (e.g., Microsoft docs, AngelSix, GitHub samples)
- Windows Service docs and TopShelf library
- FastAPI tutorials and starter templates
- Gaming addiction communities (r/StopGaming, Game Quitters)

### Appendix C: Marketing Channels & SEO

- Primary: r/StopGaming, Game Quitters, gaming parent groups, YouTube/TikTok creators
- Secondary: ProductHunt, Indie Hackers, Twitter/X, gaming Discords
- SEO keywords: "gaming addiction help", "stop playing League of Legends", "block games on PC", "gaming self-control app"

## Appendix D: Security & Tamper Model (Phase 2 focus)

- **IPC security**: Named pipes scoped to session; per-boot nonce + server-side HMAC over requests; Windows ACLs on pipe.
- **Privilege split**: UI (user) vs Service (SYSTEM). Service owns termination and persistence.
- **Emergency unlock**: 60s countdown + reason capture → local event log. No cloud PII for process names.
- **Safe Mode stance**: We log detection and educate; we don’t play “cat-and-mouse” with kernel features.
- **Integrity checks**: Service watchdog (SC recovery), file hash check on core binaries (optional), config encryption at rest (DPAPI).

## Appendix E: Privacy & GDPR Notes

- **Data minimization**: No process names or detailed activity sent to cloud by default. Cloud stores: email, hashed device id, coarse counters.
- **Lawful basis**: Contract (service delivery) + legitimate interests (quality & safety). DSR (access/delete) handled via support email.
- **Retention**: Local logs rotate (e.g., 30 days). Cloud audit events (coarse) retained ≤12 months.
- **No sensitive categories**; clear disclosures in Privacy Policy; opt-out of telemetry where feasible without breaking safety.

## Appendix F: Telemetry Plan → Success Metrics

**Local (always on, no PII):** `lock_started`, `lock_canceled`, `lock_expired`, `process_terminated`, `emergency_unlock`.  
**Cloud (coarse, optional):** daily counters per device/user: `locks_started`, `minutes_locked`, `emergency_unlocks`.  
**KPIs mapping:** Activation (first week locks ≥3), Trial→Paid (≥20–30%), Churn (<3–5%), Avg locks/user/week (≥10).

## Appendix G: Release & Rollback

- **Versioning**: Semantic versioning (M.m.p). Tag builds; publish signed MSIX.
- **Crash reporting**: Sentry DSN baked in; symbol uploads automated.
- **Rollback**: Keep previous MSIX in Releases; user doc “Revert to previous version.”
- **Hotfix**: Branch from tag, bump patch, fast QA on matrix “smoke” subset, release notes in CHANGELOG.

## Appendix H: QA Test Matrix (excerpt)

| OS | User Type | Scenario | Result |
| --- | --- | --- | --- |
| Win10 22H2 | Standard | Soft lock 15m → add/remove blocked app | Pass: countdown, immediate enforcement |
| Win11 23H2 | Admin | Hard lock 60m → reboot at T+5m | Pass: lock persists; remaining recalculated |
| Win11 24H2 | Standard | Service restart during active lock | UI reconnect ≤2s; enforcement uninterrupted |
| Win10 22H2 | Standard | Emergency unlock | 60s friction + reason; event logged |

## Appendix I: Marketing Validation (Phase 1 activities)

- Publish 1-page landing with email capture; 2 short videos (soft vs hard lock demos).
- Recruit ≥25 beta testers (r/StopGaming/GameQuitters); run 5 user interviews.
- Track signups and UTM in Plausible; convert 5 early testers to trial at MVP.
*** End Patch
---

*Version: 1.1 — Last updated November 2, 2025*
