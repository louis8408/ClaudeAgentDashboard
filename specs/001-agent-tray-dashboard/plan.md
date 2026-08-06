# Implementation Plan: Agent Tray Dashboard

**Branch**: `001-agent-tray-dashboard` | **Date**: 2026-08-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-agent-tray-dashboard/spec.md`

## Summary

A cross-platform (Windows + macOS) background application with a system
tray/menu-bar icon that presents currently detected Claude Code CLI agent
sessions as freely rearrangeable cards on a customizable virtual-desktop
surface — each card showing at a glance whether it is working, idle,
waiting for input, or ended — lets the user click a card to open an
in-window detail overlay (current output/activity, status, and actions
like Show/Dismiss) without leaving the dashboard, and raises an OS-native
notification — click-to-focus — the moment an agent stops actively working
(idle, needs input, or its session ends), never while it continues
working. Session presence/lifecycle is detected passively via OS
process/window observation; fine-grained activity is sourced from Claude
Code's own hook events via a one-time local setup step, ingested over a
loopback-only HTTP listener the app hosts. Each agent identity's card
position and the user's chosen background image persist across restarts.
Built in C#/.NET with Avalonia UI, structured as four Clean Architecture
layers (Domain/Application/Infrastructure/Presentation) with
platform-specific and hook-ingestion interop isolated entirely in
Infrastructure behind Domain-owned interfaces, developed test-first with
unit, integration, and architecture test layers per the project
constitution (Presentation-layer UI code, including this revision's
desktop/card surface, remains the one deliberate exception per the
constitution's Amendment History).

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS)

**Primary Dependencies**: Avalonia UI 12.x (cross-platform UI + built-in `TrayIcon`; `Canvas` + pointer events for the draggable card surface; `IStorageProvider` for the background-image file picker — no new package for either); Windows toast notifications via the unpackaged-app toast API; macOS `UNUserNotificationCenter` via native interop; Win32 `user32.dll` P/Invoke (window enumeration/focus) on Windows; `ntdll.dll`/`kernel32.dll` P/Invoke (`NtQueryInformationProcess` + `ReadProcessMemory` against a process's PEB, R15) for real working-directory resolution on Windows; AppKit/Core Graphics interop on macOS; a minimal loopback-only HTTP listener (.NET's built-in `HttpListener`/Kestrel minimal APIs) for ingesting Claude Code hook payloads

**Storage**: No database. In-memory `AgentSession` list only (rebuilt on startup by re-scanning processes; activity state rebuilds from `Unknown` until the next hook signal); user preferences — launch-at-login, per-agent-identity card positions, and the chosen background image path — persisted in the same local JSON settings file under the OS per-user app-data directory (`ISettingsStore`, already `JsonSettingsStore`-backed, extended rather than replaced); hook command registration is written into the user's existing Claude Code configuration file, not a dashboard-owned store

**Testing**: xUnit; `NetArchTest.Rules` for architecture tests; `coverlet.collector` for coverage; `SonarAnalyzer.CSharp` static analysis with new-code warnings as errors

**Target Platform**: Windows 10 (1809+) / Windows 11, and macOS 13 (Ventura)+; Linux architecturally unblocked but out of scope for v1

**Project Type**: Desktop application (background tray/menu-bar app, single executable, embedding a local-only HTTP listener — no externally-reachable server component)

**Performance Goals**: Agent card surface populated within 2s of tray click (SC-001); "Show" focuses the correct window within 1s (SC-002); attention notification (idle/waiting-for-input/ended) appears within 5s of that transition, with zero notifications while merely working (SC-003); activity detail overlay populated within 2s of clicking a card (SC-007); a dragged card's position and a chosen background image both survive a restart 100% of the time (SC-008/SC-009)

**Constraints**: No elevated/admin privileges required for install or normal operation; session presence/lifecycle detection remains passive (process/window observation); fine-grained activity detection is the one deliberate exception, requiring a one-time local hook registration step rather than per-session configuration or agent control (spec Assumptions, FR-013); negligible idle resource footprint (SC-006)

**Scale/Scope**: Single user, single machine; realistically 1–20 concurrently running agents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Clean Architecture Layering | Four-project structure below enforces Domain → Application → Infrastructure → Presentation with platform interop confined to Infrastructure. | PASS |
| II. Test-First (NON-NEGOTIABLE) | tasks.md (next phase) will sequence failing tests before implementation for every use case. | PASS (process commitment, enforced at task/implementation time) |
| III. Three-Layer Test Coverage | Four dedicated test projects planned: Domain/Application unit tests, Infrastructure integration tests, and a standalone architecture-test project. | PASS |
| IV. SOLID Design | Domain ports (`IAgentWatcher`, `IAgentActivityFeed`, `IWindowFocuser`, `INotifier`, `IHookRegistrar`, `ISettingsStore`) are narrow and single-purpose (ISP/SRP) — session lifecycle, in-session activity, window focus, notification, hook setup, and preferences are deliberately separate ports rather than one broad "AgentManager"; platform implementations are swappable behind them (OCP/DIP); no implementation is expected to violate LSP since each port has exactly one implementation per OS. | PASS |
| V. Code Quality Gate | `SonarAnalyzer.CSharp` + `Directory.Build.props` + SonarCloud CI scan planned as part of Setup tasks, ahead of feature-code tasks. | PASS |

One deviation requiring justification — see Complexity Tracking below: the
local HTTP listener for hook ingestion (R9) is new infrastructure
complexity introduced by this revision.

*(Re-checked post Phase 1 design: the data model and contracts confirm the
listener stays entirely inside Infrastructure behind `IAgentActivityFeed`
and `IHookRegistrar` — Domain/Application never see HTTP concerns — so the
layering gate still PASSES; the complexity itself is justified, not
eliminated, below.)*

## Project Structure

### Documentation (this feature)

```text
specs/001-agent-tray-dashboard/
├── plan.md                       # This file (/speckit-plan command output)
├── research.md                   # Phase 0 output
├── data-model.md                 # Phase 1 output
├── quickstart.md                 # Phase 1 output
├── contracts/
│   ├── domain-ports.md           # Phase 1 output
│   └── hook-event-contract.md    # Phase 1 output — the one genuine external wire contract
└── tasks.md                      # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── ClaudeAgentDashboard.Domain/
│   ├── AgentSession.cs
│   ├── SessionState.cs
│   ├── ActivityState.cs
│   ├── ActivitySignal.cs
│   ├── TerminalWindowReference.cs
│   ├── AttentionNotification.cs
│   └── Ports/
│       ├── IAgentWatcher.cs
│       ├── IAgentActivityFeed.cs
│       ├── IWindowFocuser.cs
│       ├── INotifier.cs
│       ├── IHookRegistrar.cs
│       └── ISettingsStore.cs   # extended, not replaced: gains card-position-by-agent-identity and background-image-path members alongside existing LaunchAtLoginEnabled
│
├── ClaudeAgentDashboard.Application/
│   └── UseCases/
│       ├── OpenDashboardQuery.cs
│       ├── ShowAgentCommand.cs
│       ├── ViewAgentActivityQuery.cs
│       ├── ApplyActivitySignalCommand.cs   # correlates a signal (R10) and decides whether it crosses a notify-worthy transition (R11)
│       ├── DismissAgentCommand.cs
│       └── HandleNotificationActivatedCommand.cs
│
├── ClaudeAgentDashboard.Infrastructure/
│   ├── Windows/
│   │   ├── WindowsProcessAgentWatcher.cs
│   │   ├── Win32WindowFocuser.cs
│   │   └── WindowsToastNotifier.cs
│   ├── MacOS/
│   │   ├── MacProcessAgentWatcher.cs
│   │   ├── MacWindowFocuser.cs
│   │   └── MacUserNotifier.cs
│   ├── Hooks/
│   │   ├── HookEventListener.cs        # loopback HTTP listener implementing IAgentActivityFeed (R9)
│   │   └── ClaudeCodeHookRegistrar.cs  # implements IHookRegistrar against the user's Claude Code config
│   └── Settings/
│       └── JsonSettingsStore.cs
│
└── ClaudeAgentDashboard.Presentation/
    ├── App.axaml (+ .cs)
    ├── TrayIcon/
    │   └── TrayIconController.cs
    ├── Views/
    │   ├── DesktopWindow.axaml (+ .cs)        # replaces AgentListWindow: the one main window — card canvas + overlay host + background image
    │   ├── AgentCardView.axaml (+ .cs)        # one draggable card: icon, label, status badge; raises a "clicked" event to open the overlay
    │   └── AgentDetailOverlay.axaml (+ .cs)   # replaces AgentActivityDetailView: in-window overlay (not a separate Window) — activity, status, Show/Dismiss actions
    ├── CompositionRoot.cs   # DI wiring: binds Domain ports to the OS-appropriate Infrastructure implementation
    └── Program.cs

tests/
├── ClaudeAgentDashboard.Domain.UnitTests/
├── ClaudeAgentDashboard.Application.UnitTests/
├── ClaudeAgentDashboard.Infrastructure.IntegrationTests/
└── ClaudeAgentDashboard.Architecture.Tests/
```

**Structure Decision**: Single-solution, four-project Clean Architecture
layout (Option 1 style, adapted for a desktop app rather than a
service/CLI) inside `src/`, with a parallel `tests/` tree providing one
project per test layer per the constitution. The existing
`ClaudeAgentDashboard.sln` currently references a placeholder
`ClaudeAgentDashboard\ClaudeAgentDashboard.csproj` from initial scaffolding
that does not yet exist; this stub project reference will be replaced by
the four `src/` projects (and the solution updated to include the four
`tests/` projects) as part of the Setup tasks in the next phase
(`/speckit-tasks`), not during planning.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| Local loopback HTTP listener + hook registration (R9, `IAgentActivityFeed`/`IHookRegistrar`) — a new inter-process communication surface beyond the pure OS-observation approach of the original plan | Distinguishing Working / Idle / Waiting-for-input requires knowing what is happening *inside* a Claude Code session; OS process/window observation alone (the original, simpler design) can only ever see whether a session exists, not what it's doing. Claude Code's hooks are the only reliable, officially-exposed signal for this. | Transcript-file tailing and terminal screen-scraping were both evaluated as "simpler, zero-config" alternatives and explicitly rejected (research.md R8) — undocumented/unstable file format risk and cross-terminal-emulator fragility respectively, both worse long-term costs than one well-documented local HTTP listener behind a narrow port interface. |
