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

### 3. Get notified the moment an agent finishes (User Story 3)

1. Start an agent and let it run to completion (or terminate it) while the
   dashboard window is closed and another application has focus.
2. **Expected**: An OS-native notification appears within 5 seconds of the
   process ending (SC-003), identifying which agent finished.
3. Click the notification.
4. **Expected**: The originating terminal window is brought to the
   foreground and focused, without needing to open the dashboard window
   first (FR-008).
5. Reopen the dashboard.
6. **Expected**: That agent is listed as `Finished` and remains listed
   until dismissed (FR-012).

## Automated coverage

These scenarios are backed by (see tasks.md once generated):

- **Unit tests** against Application use cases with `IAgentWatcher`,
  `IWindowFocuser`, and `INotifier` faked.
- **Integration tests** exercising the real Infrastructure implementations
  against actual local processes/windows/notifications per target OS.
- **Architecture tests** (`NetArchTest.Rules`) asserting the Domain layer
  has no outward dependencies and Infrastructure is only referenced from
  the composition root, per the constitution.
