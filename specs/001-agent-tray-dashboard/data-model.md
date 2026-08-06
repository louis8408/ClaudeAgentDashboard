# Phase 1 Data Model: Agent Tray Dashboard

All entities below live in the **Domain** layer as plain, framework-free
types (per Constitution Principle I). None reference Avalonia, Win32,
AppKit, or any Infrastructure type directly.

## AgentSession

Represents a single detected Claude Code CLI run on the local machine.

| Field | Type | Notes |
|---|---|---|
| `Id` | opaque identifier | Stable for the lifetime of the detected process; derived from OS process id + start time to avoid PID-reuse collisions. |
| `Label` | string | Identifying label derived from working directory and/or terminal title (per spec Key Entities). |
| `Status` | `AgentStatus` enum | `Running`, `Finished`. See state transitions below. |
| `StartedAt` | timestamp | When the session was first detected. |
| `FinishedAt` | timestamp? | Set when the session transitions to `Finished`; null while running. |
| `WindowReference` | `TerminalWindowReference` | The window to focus for this session. |

**Validation rules**:
- `Label` must never be empty; if no working directory/title is resolvable, a fallback label (e.g., process id) is used — the entry is never dropped for lack of a friendly name (supports spec User Story 1 always showing detected agents).
- `FinishedAt` must be null while `Status = Running` and non-null once `Status = Finished`.

**State transitions**:

```text
Running --(process no longer alive)--> Finished --(user dismisses OR app restarts)--> [removed from active list]
```

- `Running → Finished` is one-way; a session is never reopened. If the same working directory starts a new agent later, that is a new `AgentSession` with a new `Id` (per spec edge case: a changed title/directory must not fork or lose the *same* running session, but a genuinely new process is a genuinely new session).
- Per spec FR-012, a `Finished` session remains visible until explicit dismissal or app restart — there is no automatic time-based expiry.

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

## CompletionNotification

An OS-native notification raised when an `AgentSession` transitions from
`Running` to `Finished`.

| Field | Type | Notes |
|---|---|---|
| `AgentSessionId` | opaque identifier | References the `AgentSession` that finished; used on activation to resolve the correct `TerminalWindowReference` (FR-008). |
| `RaisedAt` | timestamp | When the notification was dispatched to the OS. |
| `WasDelivered` | bool | False if the OS denied/suppressed notification delivery (spec edge case: notification permission denied) — the app must still reflect status in-window regardless. |

**Relationships**:

```text
AgentSession 1 ──── 1 TerminalWindowReference
AgentSession 1 ──── 0..1 CompletionNotification   (exactly one is raised at the Running→Finished transition, if delivery succeeds)
```

## AgentStatus (enum)

```text
Running
Finished
```

No `Dismissed` status is modeled — dismissal (FR-012) removes the
`AgentSession` from the active list entirely rather than representing it as
a third status, keeping the state machine minimal per the constitution's
simplicity expectations.
