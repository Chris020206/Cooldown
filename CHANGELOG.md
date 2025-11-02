# Changelog

All notable changes to this project will be documented in this file. The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]
### Added
- Closing any already-running blocked processes at lock activation through the new `ProcessTerminator`, emitting activity entries, and prompting soft-lock toasts in both desktop and console clients.【F:src/Cooldown.Blocker.Core/BlockerEngine.cs†L3-L127】【F:src/Cooldown.Blocker.Core/ProcessTerminator.cs†L1-L49】【F:src/Cooldown.Desktop/ViewModels/MainViewModel.cs†L13-L387】【F:src/Cooldown.Desktop/Services/ToastNotificationService.cs†L1-L32】【F:BlockerPoC/Program.cs†L17-L194】
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
