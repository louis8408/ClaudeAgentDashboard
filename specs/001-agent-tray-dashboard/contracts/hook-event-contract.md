# Hook Event Wire Contract

Unlike `domain-ports.md`, this is a genuine external interface: Claude Code
CLI sessions (external processes, once the one-time setup in `IHookRegistrar`
has run — see spec FR-013, research.md R8/R9) call into the dashboard
application over this contract. It is implemented by the Infrastructure
layer's local hook listener and MUST NOT be depended on directly by Domain
or Application code — they only ever see the resulting `ActivitySignal`
(via `IAgentActivityFeed`), never the raw HTTP shape.

## Transport

- A loopback-only HTTP listener (`http://127.0.0.1:<port>`, R9) hosted by
  the running dashboard application. Not exposed beyond the local machine.
- One route per Claude Code hook event the dashboard subscribes to. Each
  registered hook command (installed by `IHookRegistrar`) forwards the
  hook's JSON payload — which Claude Code delivers to the hook command on
  stdin — as the POST body, unmodified.

## Routes

| Route | Source hook event | Resulting `ActivitySignal.HookEvent` |
|---|---|---|
| `POST /hooks/user-prompt-submit` | `UserPromptSubmit` | `UserPromptSubmit` |
| `POST /hooks/pre-tool-use` | `PreToolUse` | `PreToolUse` |
| `POST /hooks/stop` | `Stop` | `Stop` |
| `POST /hooks/notification` | `Notification` | `Notification` |
| `POST /hooks/session-end` | `SessionEnd` | `SessionEnd` |

## Payload handling

- The listener parses whatever fields Claude Code's hook payload provides
  for that event (at minimum: a working-directory field and, once
  available, a session identifier — see research.md R10/R15 for how these
  are used to correlate to an `AgentSession`). It extracts an event-specific
  summary where present (e.g., tool name for `PreToolUse`, message text for
  `Notification`) into `ActivitySignal.SummaryText`, and a `transcript_path`
  field where present into `ActivitySignal.TranscriptPath` (R16, FR-019) —
  purely passed through for display, never parsed or acted on by the
  listener itself.
- A request the listener cannot parse (unexpected shape, missing
  correlation fields) is logged and dropped with a `4xx` response — it MUST
  NOT crash the listener or the application (per `IAgentActivityFeed`'s
  contract).
- Every successfully parsed request is translated into one
  `ActivitySignal` and published via `IAgentActivityFeed.SignalReceived`;
  this route/HTTP framing never leaks past the Infrastructure
  implementation of that port.

## Setup

`IHookRegistrar.RegisterHooks` writes one hook command per row above into
the user's Claude Code configuration, each a single-line shell invocation
(`curl`/`Invoke-RestMethod`) posting stdin to the corresponding route on
the dashboard's current listener address. Re-running registration after
the listener's port changes updates these commands in place (idempotent,
per the `IHookRegistrar` contract in `domain-ports.md`).
