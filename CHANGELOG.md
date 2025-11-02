# Changelog
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
