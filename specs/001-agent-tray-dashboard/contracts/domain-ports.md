# Domain Port Contracts

This project is a desktop application, not a library/service with an
external API — its "contracts" are the Domain-owned interfaces
(Dependency Inversion, Constitution Principle IV) that Application logic
depends on and Infrastructure implements. These are the seams unit tests
fake and architecture tests enforce the direction of.

## IAgentWatcher

Detects Claude Code agent sessions on the local machine and reports
changes over time.

- `IReadOnlyCollection<AgentSession> GetCurrentSessions()` — returns every
  currently known `AgentSession` (running or finished-but-not-dismissed),
  including sessions that were already running before the app started
  (FR-002, FR-010).
- `event Action<AgentSession> SessionStarted` — raised when a new agent
  session is first detected (FR-004).
- `event Action<AgentSession> SessionFinished` — raised exactly once when a
  previously-running session transitions to `Finished` (FR-006).

**Contract**:
- Implementations MUST NOT require the monitored process to be started by
  or registered with this application (passive observation only).
- `SessionFinished` MUST fire for both normal completion and abnormal
  termination (spec edge case).
- Detection latency (time between the real-world state change and the
  corresponding event/collection update) MUST stay within the budget
  implied by SC-001/SC-003.

## IWindowFocuser

Brings the terminal window associated with an `AgentSession` to the
foreground.

- `FocusResult Focus(TerminalWindowReference reference)` — attempts to
  bring the referenced window to the foreground and give it input focus.

**Contract**:
- Returns a result distinguishing "focused successfully" from "window no
  longer available" (FR-011) — it MUST NOT throw for the ordinary "window
  was closed" case, only for genuinely exceptional failures.
- MUST be safe to call for a reference whose `IsResolvable` is already
  `false`, returning the "no longer available" result rather than
  attempting OS calls doomed to fail.

## INotifier

Raises an OS-native notification when an agent finishes, and reports back
when the user activates (clicks) it.

- `Task<bool> NotifyFinished(AgentSession session)` — raises a
  `CompletionNotification` for the given session; returns whether delivery
  succeeded (`CompletionNotification.WasDelivered`).
- `event Action<AgentSessionId> NotificationActivated` — raised when the
  user clicks a previously-raised notification, carrying the id of the
  `AgentSession` it referred to (FR-008).

**Contract**:
- MUST NOT throw when the OS denies notification permission — returns
  `false` from `NotifyFinished` instead, so the caller can fall back to
  in-window status only (spec edge case: notification permission denied).
- `NotificationActivated` MUST fire even if the main dashboard window is
  currently closed (spec User Story 3, Acceptance Scenario 3).

## ISettingsStore

Persists the small set of user preferences identified in the spec
(currently: launch-at-login).

- `bool LaunchAtLoginEnabled { get; set; }`

**Contract**:
- Reads/writes MUST be safe to call from the UI thread without blocking
  perceptibly (local file only, no network).

---

## Presentation-facing UI contract

The Application layer exposes these operations to the Presentation layer
(Avalonia tray icon + window); Presentation MUST NOT reach past Application
into Infrastructure directly (Constitution Principle I).

| Action | Trigger | Behavior |
|---|---|---|
| `OpenDashboard()` | Tray/menu-bar icon clicked | Opens the agent list window populated from `IAgentWatcher.GetCurrentSessions()` (User Story 1). |
| `ShowAgent(agentSessionId)` | "Show" button clicked on a list entry | Calls `IWindowFocuser.Focus` for that session's `TerminalWindowReference`; on failure, surfaces the FR-011 "no longer available" message instead of the window (User Story 2). |
| `DismissAgent(agentSessionId)` | User dismisses a finished entry | Removes it from the active list (FR-012). |
| `HandleNotificationActivated(agentSessionId)` | `INotifier.NotificationActivated` fires | Same behavior as `ShowAgent`, without requiring the dashboard window to be open first (User Story 3, FR-008). |
