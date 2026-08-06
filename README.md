# Claude Agent Dashboard

A cross-platform (Windows + macOS) background tray/menu-bar app that
presents Claude Code CLI agents running on your machine as freely
rearrangeable cards on a customizable desktop surface, lets you click a
card to see its current activity and jump straight to its terminal
window, and notifies you — natively, via the OS — the moment an agent
stops actively working (idle, needs input, or its session ends), never
while it's still working.

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
tray (Windows) / menu bar (macOS). Click it to open the dashboard — a
single window that acts as its own small "desktop": each detected agent
is a draggable card (icon + label + status at a glance); click a card to
open an in-window detail overlay (current activity, status, a read-only
excerpt of the agent's own transcript, and Show/Dismiss actions) without
leaving the dashboard; close the overlay to return to the card view. Drag cards anywhere — their positions persist
across restarts, keyed by the agent's label. Use "Choose background…" to
set a custom desktop background image, which also persists.

## One-time setup: activity detection

Listing agents and jumping to their windows works with zero
configuration. Distinguishing *what* an agent is doing right now
(working / idle / waiting for input) requires Claude Code to report it,
which needs a one-time hook registration: use the "Set up activity
detection…" item in the tray menu (calls `IHookRegistrar.RegisterHooks`,
which merges hook commands into your Claude Code `settings.json` — see
[`contracts/hook-event-contract.md`](specs/001-agent-tray-dashboard/contracts/hook-event-contract.md)).
Skipping this still gives you the agent cards and window-focusing;
activity just shows as `Unknown`.

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
- **Hook-to-session correlation on Windows** now resolves each tracked
  process's real working directory by reading its PEB (`NtQueryInformationProcess`
  + `ReadProcessMemory`, `WindowsWorkingDirectoryResolver`) and matches
  hook payloads against that, rather than the process's command line —
  fixed and verified live against a real running session (see
  `quickstart.md`'s validation log). A resolution failure (unsupported
  architecture, access denied) falls back to the original command-line
  substring match; when even that misses, activity stays `Unknown` as
  before — agent detection and "Show" are unaffected either way. The
  macOS equivalent (`lsof`-based cwd resolution) is implemented to the
  same contract but unverified on real hardware.
- **Tray icon bitmap renders blank on Windows** — confirmed on Windows 11
  Pro 25H2 (build 26200, the current stable release; an earlier note
  here wrongly called it Insider/Canary). The icon registers correctly
  with the shell and is clickable, but its pixel content is blank. Ruled
  out: the icon asset/format (swapped in a known-good system icon, same
  result), the exe's own icon resource (was genuinely missing — fixed by
  adding `<ApplicationIcon>` — but the live tray bitmap stayed blank
  after the fix, so that wasn't it either), DPI scaling, and stale shell
  state. Isolated to Avalonia 12.1.1's Win32 HICON construction/hand-off
  to `Shell_NotifyIcon` on this machine. See `quickstart.md`'s validation
  log for the full trail; not yet fixed.
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
