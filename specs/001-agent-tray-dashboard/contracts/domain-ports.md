# Domain Port Contracts

This project is a desktop application, not a library/service with an
external API — its "contracts" are the Domain-owned interfaces
(Dependency Inversion, Constitution Principle IV) that Application logic
depends on and Infrastructure implements. These are the seams unit tests
fake and architecture tests enforce the direction of. The one genuine
external wire contract (hook payloads Claude Code posts to this app) is
documented separately in [hook-event-contract.md](hook-event-contract.md).

## IAgentWatcher

Detects Claude Code agent sessions on the local machine and reports
lifecycle changes over time. Covers `SessionState` only (Running/Ended) —
fine-grained activity is `IAgentActivityFeed`'s responsibility.

- `IReadOnlyCollection<AgentSession> GetCurrentSessions()` — returns every
  currently known `AgentSession` (running or ended-but-not-dismissed),
  including sessions that were already running before the app started
  (FR-002, FR-010).
- `event Action<AgentSession> SessionStarted` — raised when a new agent
  session is first detected (FR-004).
- `event Action<AgentSession> SessionEnded` — raised exactly once when a
  previously-running session's process/window is confirmed gone (FR-006).

**Contract**:
- Implementations MUST NOT require the monitored process to be started by
  or registered with this application (passive observation only).
- `SessionEnded` MUST fire for both normal completion and abnormal
  termination (spec edge case), and independently of whether hooks are
  configured for that session (it is the fallback source of truth for
  session lifecycle even with no hook signal at all).
- Detection latency (time between the real-world state change and the
  corresponding event/collection update) MUST stay within the budget
  implied by SC-001/SC-003.

## IAgentActivityFeed

Reports an agent's fine-grained in-session activity (`Working` / `Idle` /
`WaitingForInput`), sourced from Claude Code hook signals (R8). Requires
the one-time hook setup described in `IHookRegistrar`; sessions with no
hooks configured simply never receive signals and stay `Unknown` (FR-013).

- `event Action<ActivitySignal> SignalReceived` — raised whenever a hook
  payload is received and parsed, before correlation to a specific
  `AgentSession` (correlation per R10 is an Application-layer concern that
  consumes this event, not this port's responsibility).

**Contract**:
- MUST NOT throw for a malformed/unrecognized payload — logs and drops it,
  since a hook command misfiring must never crash the dashboard (R9 risk).
- MUST preserve the payload's own timestamp when present, so the consumer
  can apply the "newest-timestamp-wins" rule (spec edge case, data-model.md
  `ActivitySignal.OccurredAt`).

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

Raises an OS-native notification when an agent needs attention, and
reports back when the user activates (clicks) it.

- `Task<bool> NotifyAttention(AgentSession session, AttentionReason reason)`
  — raises an `AttentionNotification` for the given session and reason;
  returns whether delivery succeeded (`AttentionNotification.WasDelivered`).
- `event Action<AgentSessionId> NotificationActivated` — raised when the
  user clicks a previously-raised notification, carrying the id of the
  `AgentSession` it referred to (FR-008).

**Contract**:
- MUST NOT throw when the OS denies notification permission — returns
  `false` from `NotifyAttention` instead, so the caller can fall back to
  in-window status only (spec edge case: notification permission denied).
- MUST NOT be called by the Application layer for a transition into
  `Working`, and MUST NOT be called twice for the same unacknowledged
  attention-needed state without an intervening `Working` period — both
  are Application-layer responsibilities (R11) that this port simply
  trusts its caller to uphold; the port itself performs no de-duplication.
- `NotificationActivated` MUST fire even if the main dashboard window is
  currently closed (spec User Story 3, Acceptance Scenario 5).

## IHookRegistrar

Installs and verifies the Claude Code hook commands the dashboard depends
on for `IAgentActivityFeed` signals (FR-013's one-time setup step).

- `bool AreHooksRegistered()` — whether the required hook commands are
  already present in the user's Claude Code configuration.
- `void RegisterHooks(Uri listenerBaseAddress)` — writes/updates the hook
  commands to point at the dashboard's local listener address (R9).

**Contract**:
- MUST be idempotent — calling `RegisterHooks` when already registered
  (e.g., the listener's port changed) updates in place rather than
  duplicating entries.
- MUST NOT remove or alter any hook entries the user configured for
  purposes other than this dashboard.

## ISettingsStore

Persists the small set of user preferences identified in the spec
(launch-at-login, and whether hook registration has been offered/declined).

- `bool LaunchAtLoginEnabled { get; set; }`

**Contract**:
- Reads/writes MUST be safe to call from the UI thread without blocking
  perceptibly (local file only, no network).

---

## Presentation-facing UI contract

The Application layer exposes these operations to the Presentation layer
(Avalonia tray icon + windows); Presentation MUST NOT reach past
Application into Infrastructure directly (Constitution Principle I).

| Action | Trigger | Behavior |
|---|---|---|
| `OpenDashboard()` | Tray/menu-bar icon clicked | Opens the agent list window populated from `IAgentWatcher.GetCurrentSessions()`, including each session's current `ActivityState` (User Story 1). |
| `ShowAgent(agentSessionId)` | "Show" button clicked on a list entry | Calls `IWindowFocuser.Focus` for that session's `TerminalWindowReference`; on failure, surfaces the FR-011 "no longer available" message instead of the window (User Story 2). |
| `ViewAgentActivity(agentSessionId)` | List entry itself clicked (not "Show") | Opens the detail view showing `ActivitySummary`, live-updating as further `ActivitySignal`s arrive for that session (User Story 4). |
| `DismissAgent(agentSessionId)` | User dismisses an ended entry | Removes it from the active list (FR-012). |
| `HandleNotificationActivated(agentSessionId)` | `INotifier.NotificationActivated` fires | Same behavior as `ShowAgent`, without requiring the dashboard window to be open first (User Story 3, FR-008). |
