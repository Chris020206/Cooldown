# Changelog

All notable changes to this project will be documented in this file.  
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project adheres to [Semantic Versioning](https://semver.org/).

---

## [Unreleased]
### Added
- Placeholder for upcoming Phase 2 features and fixes.
- All future fixes **must** reference DX-IDs from the Phase 1 Test Report (D2-01 → D2-08).

### Fixed
- No unreleased fixes documented yet.

### Changed
- No unreleased changes documented yet.

---

# Phase 2 (v0.3.x) — Planned Items from DX-ID System
These items originate from Phase 1 testing and are REQUIRED for the next release.

### Planned Fixes (DX-ID Resolutions)
- **D2-01** — Implement lock state persistence across restarts/crashes.
- **D2-02** — Redesign duration selection model (preset vs custom).
- **D2-03** — Add missing ActivityFeed entries (lock canceled / lock expired).
- **D2-04** — Add confirmation dialog when starting a new lock during an active lock.
- **D2-05** — Resolve Riot/League multi-process mapping inconsistencies.
- **D2-06** — Ensure pre-existing League processes terminate correctly even when only “League” is selected.
- **D2-07** — Add explicit UX rule for app toggling during active locks.
- **D2-08** — Update MSIX package identity from GUID to branded product identifier.

> These DX-ID requirements are formalized in the RTM (v1.0).  
> Each resolution will appear under `Fixed` in the next release (v0.3.0+).

---

## [0.2.1] - 2025-12-01
### Added
- Automatic closure of any already-running blocked apps when a soft or hard lock begins, with logging and soft-lock toasts (`BlockerEngine`, `ProcessMonitor`, `MainViewModel`, `BlockerPoC`).
- VS Code launch profile enabling debugging of the WPF desktop app without manual setup.

### Fixed
- Alignment of `Cooldown.Desktop` and MSIX packaging to `net8.0-windows` with `win-x64` runtime identifiers for consistent build output.
- Packaging project now restores WPF entry project so `project.assets.json` is generated; MSIX builds no longer fail due to missing assets.

### Known Issues (Deferred)
- See **DX-ID System** & **Phase 1 Test Report** (D2-01 → D2-08).

---

## [0.2.0] - 2025-11-02
### Added
- Shared blocking engine (`Cooldown.Blocker.Core`) with:
  - Real-time process monitoring
  - Soft and hard locks
  - Process-tree termination with WMI fallback
  - Normalization & migration of blocked app names
- Full WPF desktop experience:
  - Lock creation (soft/hard)
  - Live countdowns/end times
  - Cancel soft locks
  - Activity logging for terminated processes
  - Pre-existing process closure reporting
- Blocked app management with persistence to `%AppData%\CooldownGG\blocker-config.json`.
- Toast notifications on soft lock activation.
- Console PoC integrated with shared engine.

---

## [0.1.0] - 2025-11-01
### Added
- Initial console proof of concept demonstrating:
  - Soft vs. hard locks
  - Basic process termination functionality

---

## Link References

[Unreleased]: https://github.com/Chris020206/Cooldown/compare/v0.2.1...HEAD  
[0.2.1]: https://github.com/Chris020206/Cooldown/compare/v0.2.0...v0.2.1  
[0.2.0]: https://github.com/Chris020206/Cooldown/compare/v0.1.0...v0.2.0  
[0.1.0]: https://github.com/Chris020206/Cooldown/releases/tag/v0.1.0

