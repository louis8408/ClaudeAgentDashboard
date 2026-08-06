# Quickstart: Validating the Agent Tray Dashboard

## Prerequisites

- .NET 8 SDK installed.
- On Windows: Windows 10 version 1809+ or Windows 11.
- On macOS: macOS 13 (Ventura) or later, running the app as a built `.app`
  bundle (required for native notification authorization — see
  [research.md](research.md) R2).
- The Claude Code CLI available on PATH, so agent sessions can be started
  in a terminal for validation.

## Build & run

```bash
dotnet build ClaudeAgentDashboard.sln
dotnet run --project src/ClaudeAgentDashboard.Presentation
```

The app starts with no visible window and places an icon in the system
tray (Windows) / menu bar (macOS).

## One-time setup: register hooks (required for Working/Idle/Waiting-for-input status)

Per FR-013 and research.md R8, distinguishing an agent's fine-grained
activity requires Claude Code to report it. On first run (or via a "Set up
activity detection" action in the tray menu), the app calls
`IHookRegistrar.RegisterHooks` to add the hook commands from
[contracts/hook-event-contract.md](contracts/hook-event-contract.md) to
your Claude Code configuration. This is one-time and local — it does not
let the dashboard launch or control agents. Skipping it still gives you
User Stories 1/2 (list + focus), with activity shown as `Unknown`.

## Validation scenarios

Each scenario below maps directly to an acceptance scenario in
[spec.md](spec.md). See [data-model.md](data-model.md) for entity
definitions and [contracts/domain-ports.md](contracts/domain-ports.md) for
the interfaces exercised.

### 1. See all running agents at a glance (User Story 1)

1. Start two Claude Code CLI sessions in two separate terminal windows.
2. Click the tray/menu-bar icon.
3. **Expected**: A window opens within 2 seconds (SC-001) listing both
   sessions, each marked `Running`.
4. Start a third agent in a new terminal window without closing the
   dashboard.
5. **Expected**: The third agent appears in the list without restarting
   the app (FR-004).
6. Close all terminal windows/agents, reopen the dashboard.
7. **Expected**: An empty state is shown (Acceptance Scenario 3).

### 2. Jump straight to an agent's window (User Story 2)

1. With at least one agent running, minimize its terminal window and
   switch focus to an unrelated application.
2. Open the dashboard and click "Show" for that agent.
3. **Expected**: Within 1 second (SC-002), the correct terminal window is
   restored, brought to the foreground, and focused.
4. Close that terminal window directly (not via the dashboard).
5. Click "Show" for the now-stale entry again.
6. **Expected**: The app reports the window is no longer available
   (FR-011) rather than doing nothing or focusing the wrong window.

### 3. Get notified only when an agent needs me (User Story 3)

Prerequisite: hooks registered (see setup step above).

1. Give an agent a task and watch it work (tool calls in progress).
2. **Expected**: No notification appears while it is working (Acceptance
   Scenario 1).
3. Let it finish its turn with nothing further to do.
4. **Expected**: An OS-native notification appears within 5 seconds
   (SC-003) identifying that agent as idle.
5. Ask it something that requires a permission decision (e.g., a file
   write it needs to confirm).
6. **Expected**: A notification appears identifying that agent as waiting
   for input, distinct from the idle notification.
7. Click either notification.
8. **Expected**: The originating terminal window is brought to the
   foreground and focused, without needing to open the dashboard window
   first (FR-008), and zero notifications were raised for the "working"
   periods in between (SC-003).
9. End the session entirely (exit the CLI or close its process).
10. **Expected**: A notification appears identifying that agent's session
    as ended; reopening the dashboard shows it listed as `Ended` until
    dismissed (FR-012).

### 4. See what an agent is currently doing (User Story 4)

Prerequisite: hooks registered.

1. With an agent actively running a tool, open the dashboard and click
   that agent's list entry (not "Show").
2. **Expected**: A detail view opens within 2 seconds (SC-007) describing
   the current activity (e.g., the tool being run).
3. Leave the detail view open and let the agent's activity change (e.g.,
   it finishes the tool and starts waiting for input).
4. **Expected**: The detail view updates in place to reflect the new
   activity without being reopened.

## Skipping hook setup

Repeat scenarios 1 and 2 without registering hooks.

**Expected**: Agents are still detected and listed, "Show" still works,
and each entry's activity shows as `Unknown` rather than a guessed value
(FR-013) — no notifications for idle/waiting-for-input are raised for
those sessions, since no activity signal exists to trigger them, but
session-ended notifications (sourced from process/window observation, not
hooks) still work.

## Automated coverage

These scenarios are backed by (see tasks.md once regenerated for this
revision):

- **Unit tests** against Application use cases with `IAgentWatcher`,
  `IAgentActivityFeed`, `IWindowFocuser`, and `INotifier` faked.
- **Integration tests** exercising the real Infrastructure implementations
  against actual local processes/windows/notifications/hook payloads per
  target OS.
- **Architecture tests** (`NetArchTest.Rules`) asserting the Domain layer
  has no outward dependencies and Infrastructure is only referenced from
  the composition root, per the constitution.
