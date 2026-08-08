# Claude Agent Dashboard

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)

A cross-platform (Windows + macOS) background tray/menu-bar app that gives
you a command-center view of every Claude Code CLI agent running on your
machine: a live table of agents with their mode and status, a collapsible
fleet-summary panel (running agent count, tokens used, context window
available), one click to jump to an agent's terminal window, and native OS
notifications the moment an agent stops actively working (idle, needs
input, or its session ends) — never while it's still working.

## Download

Grab the latest installer from the
**[Releases page](https://github.com/louis8408/ClaudeAgentDashboard/releases/latest)**:

- **Windows** — `ClaudeAgentDashboardSetup-X.Y.Z.exe`. Per-user install, no
  admin rights required.
- **macOS** — `ClaudeAgentDashboard-X.Y.Z-osx-arm64.dmg` (Apple Silicon) or
  `ClaudeAgentDashboard-X.Y.Z-osx-x64.dmg` (Intel). The app is unsigned (no
  Apple Developer account) — right-click the app in `/Applications` and
  choose **Open** the first time to get past Gatekeeper.

New installers are built automatically by
[`.github/workflows/release.yml`](.github/workflows/release.yml) whenever a
`vX.Y.Z` tag is pushed.

Full requirements, design decisions, and task breakdown for each feature
live under [`specs/`](specs/) — see
[`specs/001-agent-tray-dashboard/`](specs/001-agent-tray-dashboard/) for the
original tray/card app and
[`specs/002-command-center-dashboard/`](specs/002-command-center-dashboard/)
for the table/summary-panel redesign described below.

## Build & run

Requires the .NET 8 SDK.

```bash
dotnet build ClaudeAgentDashboard.sln
dotnet test ClaudeAgentDashboard.sln
dotnet run --project src/ClaudeAgentDashboard.Presentation
```

The app starts with no visible window and places an icon in the system
tray (Windows) / menu bar (macOS). Click it to open the dashboard:

- A collapsible **fleet status** panel across the top — running agent
  count, tokens used, and context window available, each with a small
  trend graph.
- An **agent table** below it — one row per detected agent, showing a
  human-friendly name (Claude Code's own AI-generated session title, or
  the project folder name as a fallback), its permission **mode**
  (`Manual` / `Accept Edits` / `Plan` / `Auto`), and its current
  **status** (`Working` / `Idle` / `Needs Input` / `Ended` / `Unknown`).
- Click a row to open an **in-window detail overlay**: current activity,
  mode, a live, auto-scrolling chat view of the agent's own transcript,
  and Show/Dismiss actions — expandable to fill the whole window via the
  ⤢ button. Close it to return to the table.

### Settings

Right-click the tray/menu-bar icon → **Settings…** to configure:

- **Alert me when an agent** goes idle, needs input, and/or its session
  ends — pick any combination; unchecked reasons are silently skipped.
- **Appearance** — Dark or Light theme, applied live.
- **Startup & window** — launch at login, and whether closing the main
  window minimizes it to the tray instead of exiting the app.

## One-time setup: activity detection

Listing agents and jumping to their windows works with zero
configuration. Distinguishing *what* an agent is doing right now
(working / idle / waiting for input) and its permission mode requires
Claude Code to report it, which needs a one-time hook registration: use
the "Set up activity detection…" item in the tray menu (calls
`IHookRegistrar.RegisterHooks`, which merges hook commands into your
Claude Code `settings.json` — see
[`contracts/hook-event-contract.md`](specs/001-agent-tray-dashboard/contracts/hook-event-contract.md)).
Skipping this still gives you the agent table and window-focusing;
activity and mode just show as `Unknown`.

**Any Claude Code session already running when you do this stays
`Unknown` until you restart it.** Claude Code reads its hook
configuration once, at session start, and never re-reads it mid-session
(a deliberate security measure on Claude Code's part) — so registering
hooks doesn't retroactively apply to sessions that were already open.
Only sessions started *after* setup will report activity.

## Architecture

Four-layer Clean Architecture, enforced by an automated architecture test
(`ClaudeAgentDashboard.Architecture.Tests`), not just code-review convention:

```text
src/ClaudeAgentDashboard.Domain/          # entities, port interfaces — zero outward dependencies
src/ClaudeAgentDashboard.Application/     # use cases — depend only on Domain
src/ClaudeAgentDashboard.Infrastructure/  # Win32/AppKit interop, hook listener, notifiers — implements the ports
src/ClaudeAgentDashboard.Presentation/    # Avalonia UI, tray icon, composition root

tests/ClaudeAgentDashboard.Domain.UnitTests/
tests/ClaudeAgentDashboard.Application.UnitTests/
tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/
tests/ClaudeAgentDashboard.Architecture.Tests/

installer/windows/   # Inno Setup script for the Windows installer
installer/macos/     # .app bundle + .dmg packaging for macOS
```

Only `CompositionRoot` (in Presentation) is allowed to reference
Infrastructure directly — the architecture test fails the build otherwise.
See [`.specify/memory/constitution.md`](.specify/memory/constitution.md)
for the full set of project principles (Clean Architecture, TDD, SOLID,
three-layer test coverage, static analysis).

## Contributing

`master` is protected — changes land via pull request (direct pushes are
blocked), and [CI](.github/workflows/ci.yml) builds, tests, and runs
SonarCloud analysis on every PR.

## Known gaps

These are documented in code (search for "KNOWN GAP") and in
[`quickstart.md`](specs/001-agent-tray-dashboard/quickstart.md)'s
validation log, not swept under the rug:

- **Windows toast click-to-focus** only works when PowerShell 7 is
  installed. The default Windows PowerShell 5.1 cannot subscribe to WinRT
  events at all — confirmed empirically. Toast *delivery* is unaffected.
- **macOS notification click-to-focus** is not wired up. It requires
  constructing an Objective-C class at runtime and invoking a block
  parameter — judged too risky to ship unverified with no macOS hardware
  available to test against. Delivery is implemented and should work.
- **Tray icon bitmap renders blank on Windows** — confirmed on Windows 11
  Pro. The icon registers correctly with the shell and is clickable, but
  its pixel content is blank; isolated to Avalonia 12.1.1's Win32 HICON
  construction/hand-off to `Shell_NotifyIcon` on the machine this was
  developed on, not yet fixed. The app window's own title-bar icon is
  unaffected.
- **macOS was never executed on real hardware.** All macOS-specific code
  (activity/window-focus/login-item/notification implementations, and the
  `.dmg` packaging in `installer/macos/`) compiles and its tests are
  correctly skip-guarded, but none of it — including the installer itself
  — has been run on a real Mac.
- **macOS app has no custom icon yet** — the `.dmg`/`.app` ship with the
  default generic app icon; the only source art on hand (`tray-icon.ico`)
  is too small to scale up into a usable `.icns` without looking blurry.
- **Both installers are unsigned.** Windows SmartScreen and macOS
  Gatekeeper will both warn on first run; there's no code-signing
  certificate for either platform yet.

## License

[GPL-3.0](LICENSE) — you're free to use, study, modify, and redistribute
this software, including commercially; any distributed modified version
must also be licensed under GPL-3.0 and made available in source form.

## Testing

Test-first per the constitution: every Domain/Application/Infrastructure
implementation task has a preceding failing test. Presentation-layer code
is deliberately exempt (no Avalonia UI test layer defined) and is instead
validated via the manual `quickstart.md` scenarios and live screenshot QA.

```bash
dotnet test ClaudeAgentDashboard.sln
```
