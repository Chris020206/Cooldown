# Cooldown.GG Blocker Workspace

This repository contains the building blocks for Cooldown.GG, a Windows application that helps players stay disciplined by blocking games and other distracting apps during enforced focus locks. It now includes a reusable blocking engine, a modern WPF desktop experience, and the original console proof of concept for quick testing.

## Highlights

- **Shared blocking engine** – `Cooldown.Blocker.Core` exposes a reusable API for creating locks, monitoring running processes, and terminating offenders with process-tree aware enforcement, WMI fallbacks, and immediate shutdown of already-running blocked apps when a lock begins.
- **Desktop dashboard (WPF)** – `Cooldown.Desktop` provides a dark, gaming-inspired UI where users can start soft or hard locks, monitor countdowns in real time, curate the blocked app list, and review recent enforcement activity.
- **Console PoC retained** – `BlockerPoC` still offers the lightweight CLI demonstrated in early testing, now powered by the shared engine so new engine improvements automatically flow to the command-line tool.
- **Config persistence** – Both the desktop app and the PoC load or bootstrap a JSON configuration describing blocked apps and monitoring behavior. The desktop app stores user settings in `%AppData%\CooldownGG` while the PoC continues to use its local folder for easy iteration.
- **Future roadmap documented** – The long-term plan that spans Windows services, persistence, backend billing, and post-MVP features lives in [`docs/WBS.md`](docs/WBS.md).

## Project layout

```
BlockerPoC/
  BlockerPoC.csproj           -- .NET 8 console project using the shared engine
  Program.cs                  -- Menu-driven CLI powered by the shared engine
  blocker-config.json         -- Sample configuration for console runs
src/
  Cooldown.Blocker.Core/      -- Shared engine (process monitor, lock manager, termination logic)
  Cooldown.Desktop/           -- WPF desktop shell (MVVM view models, commands, services)
docs/
  WBS.md                      -- Comprehensive work breakdown structure for the product roadmap
README.md                     -- Project overview and usage instructions
```

## Prerequisites

- Windows 10/11 device with the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.
- Visual Studio 2022 (17.8+) or Rider for running the WPF application.
- Administrator privileges when executing locks so the engine can terminate protected processes.

### Required Visual Studio 2022 templates

Ensure the following project templates are installed in Visual Studio 2022 so the included solutions load without prompts:

- **WPF App (.NET)** – used by `src/Cooldown.Desktop` for the desktop shell.
- **Class Library (.NET)** – used by `src/Cooldown.Blocker.Core` for the shared blocking engine.
- **Console App (.NET)** – used by `BlockerPoC` for the command-line proof of concept.

## Running the WPF desktop app

1. Open `src/Cooldown.Desktop/Cooldown.Desktop.csproj` in Visual Studio on Windows.
2. Restore NuGet dependencies and build the project.
3. Run the `Cooldown.Desktop` project.
4. On first launch the app seeds `%AppData%\CooldownGG\blocker-config.json` with a curated list of popular game launchers.
5. Use the dashboard to:
   - Configure soft (cancelable) or hard (enforced) locks with preset or custom durations.
   - Monitor the live countdown, end time, and cancel state of the current lock.
   - Toggle, add, or remove blocked applications.
   - Review a rolling log of recently terminated processes.

The MVVM layer keeps UI state responsive and automatically persists configuration changes. Any updates to the blocked app list are immediately applied to the running engine.

## Running the console proof of concept

```bash
cd BlockerPoC
# Restore and run on Windows
 dotnet restore
 dotnet run
```

The CLI reads `blocker-config.json` from its working directory (creating it if missing), prints the watched process list, and offers a numeric menu for issuing lock commands.

## Keeping your local checkout up to date

When new fixes land in the repository, pull them into your working tree before rebuilding:

```bash
git pull --ff-only
dotnet restore src/Cooldown.Desktop/Cooldown.Desktop.csproj
dotnet build src/Cooldown.Desktop/Cooldown.Desktop.csproj
```

## Downloadable project archive

If you need a snapshot of the entire workspace without cloning, download the packaged archive generated during automation runs:

- [`Cooldown-export.zip`](../Cooldown-export.zip) — includes the full repository tree (including the `.git` directory) as captured in this branch.

If you installed the project through Visual Studio, you can achieve the same steps by choosing **Git → Pull** and then running **Restore NuGet Packages** followed by **Build → Build Solution**. Once the build succeeds, relaunch the `Cooldown.Desktop` project to pick up the latest engine updates.

## Configuration schema

The shared `BlockerConfig` class supports both legacy and new schemas. Fresh installations will emit the richer `apps` structure:

```json
{
  "apps": [
    { "name": "League of Legends", "enabled": true },
    { "name": "steam", "enabled": true }
  ],
  "checkIntervalMs": 1000,
  "enableToastNotifications": true
}
```

Only entries with `"enabled": true` are enforced by the process monitor. The desktop app saves this file to `%AppData%\CooldownGG\blocker-config.json`; the console app keeps using a local copy for quick experimentation.

## Validation summary

Manual exploratory testing on Windows 10/11 with .NET 8.0 (Visual Studio 2022) confirmed the blocker behaves as expected:

- ✅ Steam and Riot Client processes are detected and terminated in under one second while a lock is active.
- ✅ Soft locks can be canceled on demand, whereas hard locks remain enforced until their timer expires.
- ✅ Process-tree termination ensures that child processes exit with their parent; the console now clarifies when a child process has already closed.

These results validate that the core Cooldown.gg blocking engine is technically feasible and ready for integration into a production-grade Windows experience.

## Next steps

Phase 1 of the WBS focuses on rounding out the desktop UX (system tray, onboarding, installer polish). Subsequent phases address Windows service hardening, persistence, authentication, billing, and the post-MVP roadmap. See the [Cooldown.gg Work Breakdown Structure](docs/WBS.md) for the detailed plan.
[2025-11-03]
