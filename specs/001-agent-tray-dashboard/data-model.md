# Phase 1 Data Model: Agent Tray Dashboard

All entities below live in the **Domain** layer as plain, framework-free
types (per Constitution Principle I). None reference Avalonia, Win32,
AppKit, ASP.NET/HTTP, or any Infrastructure type directly.

## AgentSession

Represents a single detected Claude Code CLI run on the local machine.

| Field | Type | Notes |
|---|---|---|
| `Id` | opaque identifier | Stable for the lifetime of the detected process; derived from OS process id + start time to avoid PID-reuse collisions. |
| `Label` | string | Identifying label derived from working directory and/or terminal title (per spec Key Entities). |
| `SessionState` | `SessionState` enum | `Running`, `Ended`. Coarse lifecycle, always derivable from process/window observation alone (R3–R5). |
| `ActivityState` | `ActivityState` enum | `Unknown`, `Working`, `Idle`, `WaitingForInput`. Fine-grained in-session activity, only ever advances past `Unknown` once a hook signal has been received for this session (FR-013). Frozen/ignored once `SessionState = Ended`. |
| `ActivitySummary` | string? | Human-readable "what it's doing" text sourced from the most recent `ActivitySignal` (e.g. tool name + short input summary, or a notification/question's text). Null until the first signal arrives. Powers the User Story 4 detail view. |
| `ActivityChangedAt` | timestamp? | Timestamp of the signal that produced the current `ActivityState`/`ActivitySummary`, used to reject out-of-order/delayed signals (R10, spec edge case). |
| `StartedAt` | timestamp | When the session was first detected. |
| `EndedAt` | timestamp? | Set when `SessionState` becomes `Ended`; null while running. |
| `WindowReference` | `TerminalWindowReference` | The window to focus for this session. |

**Validation rules**:
- `Label` must never be empty; if no working directory/title is resolvable, a fallback label (e.g., process id) is used — the entry is never dropped for lack of a friendly name (supports User Story 1 always showing detected agents).
- `EndedAt` must be null while `SessionState = Running` and non-null once `SessionState = Ended`.
- A new `ActivitySignal` only updates `ActivityState`/`ActivitySummary`/`ActivityChangedAt` if its own timestamp is *newer* than the session's current `ActivityChangedAt` (out-of-order protection, spec edge case).
- Once `SessionState = Ended`, further `ActivitySignal`s for that session are ignored — a session is never reopened.

**State transitions**:

```text
SessionState:  Running --(process/session no longer alive)--> Ended --(dismiss OR app restart)--> [removed from active list]

ActivityState (only while SessionState = Running):
  Unknown --(first signal, any kind)--> {Working | Idle | WaitingForInput}
  Working <--> Idle <--> WaitingForInput   (any transition possible; see R8 signal → state mapping)
```

- `SessionState: Running → Ended` is one-way; a session is never reopened. If the same working directory starts a new agent later, that is a new `AgentSession` with a new `Id` (per spec edge case: a changed title/directory must not fork or lose the *same* running session, but a genuinely new process is a genuinely new session).
- Per spec FR-012, an `Ended` session remains visible until explicit dismissal or app restart — there is no automatic time-based expiry.
- `ActivityState` has no forced ordering between `Working`/`Idle`/`WaitingForInput` — any is reachable from any other, driven purely by which hook fires next (R8).

## TerminalWindowReference

The OS-level window associated with the terminal hosting an `AgentSession`,
used to bring that window to the foreground.

| Field | Type | Notes |
|---|---|---|
| `OwningProcessId` | int | OS process id of the detected agent process (or its terminal host process, per R4 in research.md). |
| `PlatformHandle` | opaque handle | Win32 `HWND` on Windows, or the owning application's identifier on macOS — never exposed outside Infrastructure; Domain only holds an opaque reference `IWindowFocuser` can resolve. |
| `IsResolvable` | bool | False once the window has been confirmed closed (supports FR-011: inform the user rather than fail silently). |

**Validation rules**:
- `IsResolvable` transitions from `true` to `false` only; once a window reference is confirmed gone it is not resurrected (a new `Show`/notification attempt against a dead reference must surface the "window no longer available" outcome from FR-011).

## ActivitySignal

Represents a single received hook invocation, used to derive an
`AgentSession`'s `ActivityState`/`ActivitySummary`. Transient — the
application folds each signal into its target `AgentSession` and does not
need to retain a full history for v1.

| Field | Type | Notes |
|---|---|---|
| `CorrelationKey` | value (cwd, and session id once known) | Used to match this signal to an `AgentSession` (R10). |
| `HookEvent` | `HookEvent` enum | `UserPromptSubmit`, `PreToolUse`, `Stop`, `Notification`, `SessionEnd` — see contracts/hook-event-contract.md for the wire shape each maps from. |
| `OccurredAt` | timestamp | From the hook payload if it carries one, else the time the signal was received. |
| `SummaryText` | string? | Short human-readable text for this signal (tool name + input summary for `PreToolUse`; the notification text for `Notification`; null for purely state-transition events). |

**Mapping to `ActivityState`** (R8): `UserPromptSubmit`/`PreToolUse` → `Working`; `Stop` → `Idle`; `Notification` → `WaitingForInput`; `SessionEnd` → forces `SessionState = Ended` (and makes `ActivityState` moot).

## AttentionNotification

An OS-native notification raised when an `AgentSession` transitions into
`Idle`, `WaitingForInput`, or `Ended` (never for `Working`), per FR-007/R11.
Replaces the earlier, narrower "CompletionNotification" concept now that
more transitions can raise one.

| Field | Type | Notes |
|---|---|---|
| `AgentSessionId` | opaque identifier | References the `AgentSession` this notification is about; used on activation to resolve the correct `TerminalWindowReference` (FR-008). |
| `Reason` | `AttentionReason` enum | `Idle`, `WaitingForInput`, `Ended` — why this notification was raised; shown in the notification text. |
| `RaisedAt` | timestamp | When the notification was dispatched to the OS. |
| `WasDelivered` | bool | False if the OS denied/suppressed notification delivery (spec edge case: notification permission denied) — the app must still reflect status in-window regardless. |

**Relationships**:

```text
AgentSession 1 ──── 1 TerminalWindowReference
AgentSession 1 ──── 0..* ActivitySignal        (folded in as received; no long-term history retained in v1)
AgentSession 1 ──── 0..* AttentionNotification (at most one per distinct transition into Idle/WaitingForInput/Ended, per R11 de-duplication)
```

## Enums

```text
SessionState:   Running, Ended
ActivityState:  Unknown, Working, Idle, WaitingForInput
HookEvent:      UserPromptSubmit, PreToolUse, Stop, Notification, SessionEnd
AttentionReason: Idle, WaitingForInput, Ended
```

No `Dismissed` status is modeled — dismissal (FR-012) removes the
`AgentSession` from the active list entirely rather than representing it as
a state value, keeping the state machine minimal per the constitution's
simplicity expectations.
