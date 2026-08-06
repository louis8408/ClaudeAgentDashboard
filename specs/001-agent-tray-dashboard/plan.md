# Implementation Plan: Agent Tray Dashboard

**Branch**: `001-agent-tray-dashboard` | **Date**: 2026-08-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-agent-tray-dashboard/spec.md`

## Summary

A cross-platform (Windows + macOS) background application with a system
tray/menu-bar icon that lists currently running Claude Code CLI agent
sessions, lets the user jump to any agent's terminal window, and raises an
OS-native notification — click-to-focus — when an agent finishes. Built in
C#/.NET with Avalonia UI, structured as four Clean Architecture layers
(Domain/Application/Infrastructure/Presentation) with platform-specific
process/window/notification interop isolated entirely in Infrastructure
behind Domain-owned interfaces, developed test-first with unit,
integration, and architecture test layers per the project constitution.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS)

**Primary Dependencies**: Avalonia UI 11.x (cross-platform UI + built-in `TrayIcon`); Windows toast notifications via the unpackaged-app toast API; macOS `UNUserNotificationCenter` via native interop; Win32 `user32.dll` P/Invoke (window enumeration/focus) on Windows; AppKit/Core Graphics interop on macOS

**Storage**: No database. In-memory `AgentSession` list only (rebuilt on startup by re-scanning processes); one user preference (launch-at-login) persisted in a local JSON settings file under the OS per-user app-data directory

**Testing**: xUnit; `NetArchTest.Rules` for architecture tests; `coverlet.collector` for coverage; `SonarAnalyzer.CSharp` static analysis with new-code warnings as errors

**Target Platform**: Windows 10 (1809+) / Windows 11, and macOS 13 (Ventura)+; Linux architecturally unblocked but out of scope for v1

**Project Type**: Desktop application (background tray/menu-bar app, single executable, no server component)

**Performance Goals**: Agent list populated within 2s of tray click (SC-001); "Show" focuses the correct window within 1s (SC-002); completion notification appears within 5s of process exit (SC-003)

**Constraints**: No elevated/admin privileges required for install or normal operation; passive observation only — must not require modifying or instrumenting the Claude Code CLI itself (spec Assumptions); negligible idle resource footprint (SC-006)

**Scale/Scope**: Single user, single machine; realistically 1–20 concurrently running agents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Clean Architecture Layering | Four-project structure below enforces Domain → Application → Infrastructure → Presentation with platform interop confined to Infrastructure. | PASS |
| II. Test-First (NON-NEGOTIABLE) | tasks.md (next phase) will sequence failing tests before implementation for every use case. | PASS (process commitment, enforced at task/implementation time) |
| III. Three-Layer Test Coverage | Four dedicated test projects planned: Domain/Application unit tests, Infrastructure integration tests, and a standalone architecture-test project. | PASS |
| IV. SOLID Design | Domain ports (`IAgentWatcher`, `IWindowFocuser`, `INotifier`, `ISettingsStore`) are narrow and single-purpose (ISP/SRP); platform implementations are swappable behind them (OCP/DIP); no implementation is expected to violate LSP since each port has exactly one implementation per OS. | PASS |
| V. Code Quality Gate | `SonarAnalyzer.CSharp` + `Directory.Build.props` + SonarCloud CI scan planned as part of Setup tasks, ahead of feature-code tasks. | PASS |

No deviations — Complexity Tracking table is empty (see below).

*(Re-checked post Phase 1 design: the data model and contracts introduced no new external dependencies or layering exceptions — gate still PASSES.)*

## Project Structure

### Documentation (this feature)

```text
specs/001-agent-tray-dashboard/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── domain-ports.md  # Phase 1 output
└── tasks.md              # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── ClaudeAgentDashboard.Domain/
│   ├── AgentSession.cs
│   ├── AgentStatus.cs
│   ├── TerminalWindowReference.cs
│   ├── CompletionNotification.cs
│   └── Ports/
│       ├── IAgentWatcher.cs
│       ├── IWindowFocuser.cs
│       ├── INotifier.cs
│       └── ISettingsStore.cs
│
├── ClaudeAgentDashboard.Application/
│   └── UseCases/
│       ├── OpenDashboardQuery.cs
│       ├── ShowAgentCommand.cs
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
│   └── Settings/
│       └── JsonSettingsStore.cs
│
└── ClaudeAgentDashboard.Presentation/
    ├── App.axaml (+ .cs)
    ├── TrayIcon/
    │   └── TrayIconController.cs
    ├── Views/
    │   └── AgentListWindow.axaml (+ .cs)
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

*No Constitution Check violations — this section intentionally left
without entries.*
