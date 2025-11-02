# Changelog

All notable changes to this project will be documented in this file. The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]
### Added
- Closing any already-running blocked processes the moment a soft or hard lock activates, logging each termination, and surfacing soft-lock toasts in the desktop and console clients.【F:src/Cooldown.Blocker.Core/BlockerEngine.cs†L58-L113】【F:src/Cooldown.Blocker.Core/ProcessMonitor.cs†L12-L118】【F:src/Cooldown.Desktop/ViewModels/MainViewModel.cs†L40-L361】【F:BlockerPoC/Program.cs†L19-L186】
### Planned
- Deliver installer, onboarding, and system tray polish for the WPF desktop experience ahead of beta launch.【F:docs/WBS.md†L93-L99】【F:README.md†L98-L100】
- Stand up the Windows service with persistent storage and resilient IPC to harden lock enforcement.【F:docs/WBS.md†L48-L49】【F:docs/WBS.md†L164-L172】
- Implement authentication and subscription billing flows backed by Stripe webhooks and entitlement checks.【F:docs/WBS.md†L49-L50】【F:docs/WBS.md†L220-L228】

## [0.2.0] – 2025-11-02
### Added
- Shared blocking engine (`Cooldown.Blocker.Core`) combining `ProcessMonitor`, `LockManager`/`LockState`/`LockType`, and `ProcessKiller` to deliver config-driven, sub-second process enforcement with process-tree termination and WMI fallback when locks are active.【F:src/Cooldown.Blocker.Core/ProcessMonitor.cs†L5-L113】【F:src/Cooldown.Blocker.Core/LockManager.cs†L3-L145】【F:src/Cooldown.Blocker.Core/ProcessKiller.cs†L6-L53】【F:src/Cooldown.Blocker.Core/BlockerEngine.cs†L3-L112】
- WPF desktop application (`Cooldown.Desktop`) with MVVM view models, async commands, and a dark gaming dashboard for issuing soft/hard locks, tracking live countdowns, managing the blocked app list, and reviewing a rolling activity feed.【F:src/Cooldown.Desktop/ViewModels/MainViewModel.cs†L12-L361】【F:src/Cooldown.Desktop/Commands/AsyncRelayCommand.cs†L5-L50】【F:src/Cooldown.Desktop/Commands/RelayCommand.cs†L5-L23】【F:src/Cooldown.Desktop/Views/MainWindow.xaml†L1-L155】
- Configuration persistence service that saves normalized `BlockerConfig` data (including `.exe` stripping and legacy migration) to `%AppData%\CooldownGG\blocker-config.json`, keeping desktop and console experiences aligned.【F:src/Cooldown.Desktop/Services/BlockerConfigService.cs†L7-L44】【F:src/Cooldown.Blocker.Core/BlockerConfig.cs†L5-L104】
- Refreshed console proof of concept that reuses the shared engine, offers menu shortcuts for soft/hard locks, prints lock status, and logs termination outcomes with contextual icons.【F:BlockerPoC/Program.cs†L17-L187】【F:BlockerPoC/blocker-config.json†L1-L17】

### Changed
- Console configuration now normalizes executable names and reports when child processes were already closed, producing clearer status messaging during enforcement runs.【F:src/Cooldown.Blocker.Core/BlockerConfig.cs†L73-L84】【F:BlockerPoC/Program.cs†L175-L186】

## [0.1.0] – 2025-11-01
### Added
- Initial console proof of concept validating soft versus hard locks, sub-second detection against Steam and Riot processes, and process-tree termination reliability to exit child tasks cleanly.【F:README.md†L90-L96】
- Documented next steps outlining Phase 1 desktop UX work and Phase 2 service/persistence hardening to guide the transition from PoC to MVP.【F:README.md†L98-L100】【F:docs/WBS.md†L48-L99】

[Unreleased]: https://github.com/cooldown-gg/cooldown/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/cooldown-gg/cooldown/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/cooldown-gg/cooldown/releases/tag/v0.1.0
All notable changes to this project will be documented in this file.

The format is based on **[Keep a Changelog](https://keepachangelog.com/en/1.1.0/)**  
and this project adheres to **[Semantic Versioning](https://semver.org/spec/v2.0.0.html)**.

---

## [Unreleased]

### Added
- 🧭 Project documentation: `docs/WBS.md` (Work Breakdown Structure) and `README.md` expansion.
- 🧪 Initial test data and seeds for desktop config creation.
- 🔔 Activity feed in the desktop UI showing recent blocked processes.

### Changed
- UI polish: dark gaming theme, layout balance, copy tweaks for clarity.
- Engine event wiring improved to surface lock state and block events to UI.

### Fixed
- Robust handling for processes that exit between enumeration and kill attempts.
- Timer edge cases for countdown reaching zero.

---

## [0.2.0] - 2025-11-02 — **Desktop Shell + Shared Core**
**Highlights:** Modern WPF app (MVVM), shared blocking engine library, config persistence, and a cleaner PoC wired to the core.

### Added
- **Shared Core (`src/Cooldown.Blocker.Core`)**
  - `BlockerEngine` with events for `LockStateChanged` and `ProcessBlocked`.
  - `ProcessMonitor` with seen-PID tracking and adjustable scan interval.
  - `ProcessKiller` with `Process.Kill(entireProcessTree: true)` and WMI fallback.
  - `LockManager`, `LockState`, `LockType` with soft/hard locks, cancel rules, and a 1s tick timer.
  - `BlockerConfig` + `BlockableApp` with legacy schema migration and normalization.
- **WPF Desktop (`src/Cooldown.Desktop`)**
  - MVVM: `MainViewModel`, `LockStatusViewModel`, `BlockedAppViewModel`, `ObservableObject`, commands.
  - Views: `MainWindow.xaml` with Lock Control, Blocked Apps, Activity panels.
  - Services: `BlockerConfigService` (AppData JSON), `BlockerEngineHost` (lifecycle and events).
  - UX: preset/custom durations, soft/hard selection, live countdown, cancel for soft locks.
  - Activity log (last 50 events).
- **Console PoC refresh (`BlockerPoC/`)**
  - Menu: 5-min soft, 60-min hard, status, cancel, exit.
  - JSON config bootstrap + pretty print.

### Changed
- PoC now uses the shared core library so improvements flow to both desktop and console.
- Config schema updated to `apps: [ { name, enabled } ]` with auto-migration from `blockedProcessNames`.

### Fixed
- Stability during process enumeration errors and already-exited PIDs.
- Accurate time remaining display (minutes/seconds) and lock end formatting.

---

## [0.1.0] - 2025-11-01 — **Proof of Concept**
**Highlights:** Validated feasibility and reliability of enforced blocking.

### Added
- Console blocker that detects and terminates target processes in < 1s during active locks.
- Soft (cancelable) and hard (non-cancelable) locks.
- Multi-app support (Steam, Riot Client, etc.) with child-process termination.
- Basic test report confirming 100% blocking success in manual trials.

---

## Release Process
1. Update **Unreleased** entries and move them under a new version section.
2. Bump version using SemVer:
   - **MAJOR**: breaking changes
   - **MINOR**: new features, backwards compatible
   - **PATCH**: fixes and small improvements
3. Tag the release:
   ```bash
   git tag -a vX.Y.Z -m "vX.Y.Z"
   git push origin vX.Y.Z
