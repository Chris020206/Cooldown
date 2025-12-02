# Changelog

All notable changes to this project will be documented in this file. The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]
### Added
- No unreleased changes documented yet.

## [0.2.1] - 2025-12-01
### Added
- Closing any already-running blocked processes when a soft or hard lock activates, logging each termination, and surfacing soft-lock toasts in the desktop and console clients (`BlockerEngine`, `ProcessMonitor`, `MainViewModel`, `BlockerPoC`).
- VS Code launch profile for debugging the WPF desktop app without manual configuration.
### Fixed
- Aligned `Cooldown.Desktop` and MSIX packaging to `net8.0-windows` with `win-x64` runtime identifiers so restore/build output stays consistent across projects.
- Ensured the WAP packaging project restores the entry WPF project so `project.assets.json` is generated and MSIX builds no longer fail with missing assets.

## [0.2.0] - 2025-11-02
### Added
- Shared blocking engine (`src/Cooldown.Blocker.Core`) with process monitoring, lock manager/timer, process-tree termination with WMI fallback, and normalization of blocked app names (including legacy schema migration).
- WPF desktop experience (`src/Cooldown.Desktop`) that creates soft/hard locks with preset or custom durations, shows live countdowns/end times, allows canceling soft locks, logs terminated processes, and reports pre-existing process closures at lock activation.
- Blocked app management with persistence to `%AppData%\CooldownGG\blocker-config.json`, default seeds for popular launchers, and toast notifications when soft locks close already-running apps.
- Console proof of concept (`BlockerPoC`) wired to the shared engine with menu options for starting/canceling locks and checking status.

## [0.1.0] - 2025-11-01
### Added
- Initial console proof of concept demonstrating soft vs. hard locks and process termination for selected apps.

[Unreleased]: https://github.com/Chris020206/Cooldown/compare/v0.2.1...HEAD
[0.2.1]: https://github.com/Chris020206/Cooldown/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/Chris020206/Cooldown/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/Chris020206/Cooldown/releases/tag/v0.1.0
