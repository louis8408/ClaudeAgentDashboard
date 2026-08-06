# Claude Agent Dashboard

A cross-platform (Windows + macOS) background tray/menu-bar app that lists
Claude Code CLI agents running on your machine, lets you jump straight to
an agent's terminal window, and notifies you — natively, via the OS — the
moment an agent stops actively working (idle, needs input, or its session
ends), never while it's still working.

Full requirements, design decisions, and task breakdown live under
[`specs/001-agent-tray-dashboard/`](specs/001-agent-tray-dashboard/):
[`spec.md`](specs/001-agent-tray-dashboard/spec.md) (what/why),
[`plan.md`](specs/001-agent-tray-dashboard/plan.md) +
[`research.md`](specs/001-agent-tray-dashboard/research.md) (how),
[`tasks.md`](specs/001-agent-tray-dashboard/tasks.md) (build order), and
[`quickstart.md`](specs/001-agent-tray-dashboard/quickstart.md) (manual
validation scenarios and the validation log from this machine).

## Build & run

Requires the .NET 8 SDK.

```bash
dotnet build ClaudeAgentDashboard.sln
dotnet test ClaudeAgentDashboard.sln
dotnet run --project src/ClaudeAgentDashboard.Presentation
```

The app starts with no visible window and places an icon in the system
tray (Windows) / menu bar (macOS). Click it to see the agent list.

## One-time setup: activity detection

Listing agents and jumping to their windows works with zero
configuration. Distinguishing *what* an agent is doing right now
(working / idle / waiting for input) requires Claude Code to report it,
which needs a one-time hook registration: use the "Set up activity
detection…" item in the tray menu (calls `IHookRegistrar.RegisterHooks`,
which merges hook commands into your Claude Code `settings.json` — see
[`contracts/hook-event-contract.md`](specs/001-agent-tray-dashboard/contracts/hook-event-contract.md)).
Skipping this still gives you the agent list and window-focusing; activity
just shows as `Unknown`.

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
```

Only `CompositionRoot` (in Presentation) is allowed to reference
Infrastructure directly — the architecture test fails the build otherwise.
See [`.specify/memory/constitution.md`](.specify/memory/constitution.md)
for the full set of project principles (Clean Architecture, TDD, SOLID,
three-layer test coverage, static analysis).

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
- **Hook-to-session correlation** is a plain substring match of a hook's
  working directory against the detected process's command line. On
  Windows this often won't match (WMI exposes no per-process working
  directory without reading the process's PEB, which was evaluated and
  deferred — see `research.md` R10). When it misses, activity just stays
  `Unknown`; agent detection and "Show" are unaffected.
- **Tray icon visibility could not be confirmed** on the Windows machine
  this was built on (Windows 11 build 26200, an Insider/Canary-channel
  build). Extensive empirical debugging (see `quickstart.md`) isolated
  this to the OS/Avalonia rendering path, not application code — ruled
  out by testing with a known-good Windows system icon and getting the
  identical result. Needs re-validation on a standard Windows release.
- **macOS was never executed.** All macOS-specific code compiles and its
  tests are correctly skip-guarded, but none of it has run on real
  hardware in this session.

## Testing

Test-first per the constitution: every Domain/Application/Infrastructure
implementation task has a preceding failing test. Presentation-layer code
is deliberately exempt (no Avalonia UI test layer defined) and is instead
validated via the manual `quickstart.md` scenarios.

```bash
dotnet test ClaudeAgentDashboard.sln
```
