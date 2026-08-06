# Phase 0 Research: Agent Tray Dashboard

## R1: Tray/menu-bar icon and popover window

- **Decision**: Use Avalonia UI's built-in `TrayIcon` control (`Avalonia.Controls`) to host the system tray icon (Windows) / menu bar icon (macOS), opening an Avalonia window as the agent list popover on click.
- **Rationale**: Avalonia's `TrayIcon` API is cross-platform out of the box (single implementation covers both target OSes), avoiding a bespoke native tray integration per platform for this piece.
- **Alternatives considered**: Separate native tray libraries per OS (`System.Windows.Forms.NotifyIcon` on Windows + a bespoke `NSStatusItem` wrapper on macOS) — rejected, duplicates cross-platform UI work Avalonia already solves and would require two codepaths for the same concern. Electron/Tauri — rejected earlier in project discussion in favor of C#/.NET + Avalonia.

## R2: OS-native completion notifications

- **Decision**: Implement `INotifier` (Domain-owned port) with two Infrastructure implementations selected at composition-root startup by OS: Windows uses toast notifications via the unpackaged-app toast APIs (`Windows.UI.Notifications.ToastNotificationManager` with an app-registered AppUserModelID, no MSIX packaging required, supported from Windows 10 version 1809+); macOS uses `UNUserNotificationCenter` via a thin native interop shim, requiring the app to run as a proper `.app` bundle with a valid bundle identifier so macOS grants notification authorization.
- **Rationale**: Both are genuinely native, OS-owned notification centers (matches the spec's "OS-native notification" requirement, FR-007) and both support a click-activation callback needed for FR-008.
- **Alternatives considered**: Shelling out to `osascript -e 'display notification'` on macOS — rejected as the primary path because the notification is attributed to the shell/script host rather than the app, and it does not reliably deliver a click-activation callback back into the app; kept only as a documented fallback if native interop proves infeasible during implementation. Classic Windows Forms balloon tips — rejected, not a native toast and visually/behaviorally deprecated.
- **Risk noted**: macOS notification authorization must be requested and can be denied by the user; this is already covered by spec edge case "OS denies notification permission" (FR — app must still reflect status in-window).

## R3: Detecting running Claude Code agent processes

- **Decision**: Enumerate local processes and match on the Claude Code CLI's process signature (process name plus command-line arguments, since the CLI runs as a Node-hosted process). Windows: query full command lines via WMI (`Win32_Process.CommandLine`), since `System.Diagnostics.Process` alone does not expose another process's command line. macOS: shell out to `ps -o pid=,command=` for the current user's processes (no elevated privileges required to read one's own processes' command lines).
- **Rationale**: Matches the spec's Assumption of passive, zero-configuration detection — no changes to Claude Code itself are required (out of scope per spec).
- **Alternatives considered**: Watching Claude Code's own session/log files — rejected as a v1 primary mechanism since it depends on undocumented, potentially unstable file formats/locations; process-based detection is more robust to Claude Code internal changes. A companion hook/plugin invoked by Claude Code on start/stop — rejected, contradicts the spec's "observe already-running agents" scope (would require configuring Claude Code itself).

## R4: Mapping a process to its terminal window, and bringing it to the foreground

- **Decision**: Windows — enumerate top-level windows via `EnumWindows` + `GetWindowThreadProcessId` (user32.dll P/Invoke) to find the window owned by the detected process (or its parent conhost/terminal host process), then call `SetForegroundWindow`/`ShowWindow` to focus it, applying the standard `AttachThreadInput` workaround for the Windows foreground-lock restriction when the caller isn't the currently active thread. macOS — resolve the owning application via its PID and use `NSRunningApplication.activateWithOptions` (Objective-C interop) to bring that application's window forward.
- **Rationale**: These are the standard, well-documented mechanisms for cross-application window focusing on each OS; no third-party cross-platform window-manager library is mature enough on .NET to cover both OSes reliably, so this is implemented directly behind the Domain-owned `IWindowFocuser` port.
- **Risk / scope boundary**: Focus resolves at the OS top-level window granularity. When a terminal emulator hosts multiple sessions as tabs within one OS window, focusing brings the whole window forward but cannot guarantee switching to the specific tab — this matches the spec's documented Assumption (one agent per terminal window is the supported case for v1); tab-level disambiguation for multiplexed terminals is explicitly out of scope for v1.

## R5: Detecting when an agent finishes

- **Decision**: Poll each tracked process's liveness at a short fixed interval (~1–2 seconds) rather than relying solely on .NET's `Process.Exited` event for externally-started (non-child) processes.
- **Rationale**: Reliable event-driven exit notification for a process this app did not start is not uniformly guaranteed across Windows and macOS in .NET; a short poll interval comfortably satisfies the spec's 5-second notification SLA (SC-003) with margin while remaining simple and portable.
- **Alternatives considered**: `Process.EnableRaisingEvents` + `Exited` event only — rejected as the sole mechanism due to inconsistent behavior for non-child processes across platforms; may still be layered on top as an optimization later without changing the `IAgentWatcher` contract.

## R6: Persistence

- **Decision**: No database. The active agent-session list is in-memory only (rebuilt by re-scanning processes on startup, per spec FR-010). The one user preference identified during specification (launch-at-login) is persisted in a small local JSON settings file under the OS-standard per-user app-data directory.
- **Rationale**: Matches the spec's scope (single user, single machine, no historical reporting required); avoids an unjustified storage dependency.
- **Alternatives considered**: SQLite/local database — rejected as unnecessary complexity for a single small settings value and a transient in-memory session list.

## R7: Testing stack

- **Decision**: xUnit as the test framework across all layers; `NetArchTest.Rules` for the architecture-test layer enforcing the constitution's dependency-direction rule; `coverlet.collector` for coverage collection; `SonarAnalyzer.CSharp` wired via `Directory.Build.props` for static analysis with new-code warnings treated as errors.
- **Rationale**: Directly satisfies the constitution's Principle III (three test layers) and Principle V (code quality gate) with idiomatic, well-supported .NET tooling.
- **Alternatives considered**: NUnit/MSTest — no material advantage for this project; xUnit chosen for its wide .NET 8 ecosystem support and parallel-by-default test execution.

## R8: Detecting agent activity state (Working / Idle / Waiting-for-input)

- **Decision**: Register commands for Claude Code's built-in lifecycle hooks as a one-time setup step: `UserPromptSubmit` and `PreToolUse` map to `Working`; `Stop` maps to `Idle`; `Notification` maps to `WaitingForInput` (covers both permission requests and Claude Code's own "waiting for your input" idle notice); `SessionEnd` maps to `Ended`, supplementing process-exit detection (R5) as a second, more immediate signal for the same transition.
- **Rationale**: This is the only signal that reflects what is actually happening *inside* a session — process/window observation (R3/R4/R5) can tell you a session is running or has ended, but not whether it is thinking, idle at the prompt, or blocked on a question. Hooks are the mechanism Claude Code itself exposes for exactly this purpose, and were selected over the alternatives below after explicit evaluation.
- **Alternatives considered**: Watching Claude Code's session transcript files for content changes — rejected, undocumented/unstable format, and the point of this decision was to get a reliable signal, not trade one guess for another. Terminal screen-scraping for known prompt text — rejected, fragile across terminal emulators/fonts/themes and the highest ongoing maintenance cost of the options considered.
- **Scope note**: This is the one deliberate, narrow exception to the original "no changes to Claude Code required" assumption — a one-time local configuration step (registering hooks), not per-session setup, and not a way for the dashboard to control or launch agents (spec Assumptions). An agent for which this step hasn't been done is still detected and listed via R3/R4/R5 alone, with its fine-grained activity shown as `Unknown` (FR-013).

## R9: Local event ingestion transport for hook payloads

- **Decision**: Host a loopback-only HTTP listener inside the running application (bound to `127.0.0.1` on a fixed local port, falling back to a nearby free port if occupied), using .NET's built-in minimal HTTP server support — no new IPC technology. The registered hook commands are single-line shell commands (`curl`/PowerShell's `Invoke-RestMethod`) that forward the hook's JSON payload (delivered on stdin per Claude Code's hook contract) to one route per hook event type on this listener.
- **Rationale**: Works identically on Windows and macOS, needs no elevated privileges (high port, loopback-only), and keeps each hook command a trivial one-liner rather than requiring a separate compiled helper binary to build, ship, and keep on `PATH`.
- **Alternatives considered**: A named pipe / Unix domain socket — rejected, would need a small companion executable per OS to bridge the hook's shell invocation into the pipe, more moving parts than an HTTP POST from tools already on most systems. A dedicated CLI helper binary instead of a raw `curl` one-liner — deferred; could replace the one-liner later without changing the wire contract in `contracts/hook-event-contract.md`.
- **Risk noted**: The listener is unauthenticated (loopback-only limits exposure to the local machine); it must reject unparseable payloads without crashing, and a failed port bind must surface as a visible startup error rather than silently dropping all activity signals.

## R10: Correlating a hook event with an already-tracked Agent Session

- **Decision**: Match an incoming hook event to the `AgentSession` already known via `IAgentWatcher` (R3/R4) primarily by the hook payload's `cwd` (working directory); once a `session_id` has been seen for a given `AgentSession`, later events for that `session_id` are matched directly without re-matching on `cwd`. A hook event whose `cwd` doesn't yet match any tracked session is held briefly and re-matched as new sessions appear, rather than discarded, to avoid missing the first signal for a just-started agent.
- **Rationale**: Hook payloads carry `session_id`/`cwd`/`transcript_path` but no raw OS process id, so process-based detection (R3) and hook-based activity detection (R8) need a shared correlation key; working directory is the field common to both and consistent with the existing "one agent per terminal window" v1 scope.
- **Alternatives considered**: Treating the hook `session_id` as the sole source of session identity, dropping process/window watching — rejected, hook payloads have no reliable pointer to the OS-level terminal window that "Show" (US2) needs; the two signals are complementary, not substitutable.

## R11: Notification triggering and de-duplication

- **Decision**: Raise an `AttentionNotification` on a transition of `ActivityState`/`SessionState` into `Idle`, `WaitingForInput`, or `Ended`, only when arriving from a genuinely different state, and never for a transition into `Working`. A session already sitting in an unacknowledged `Idle`/`WaitingForInput` state that moves directly between those two without an intervening `Working` period does not raise a second notification (spec FR-007a).
- **Rationale**: Directly implements "notify me only when it needs me" and avoids notification fatigue from rapid Idle↔WaitingForInput flapping the user explicitly anticipated.
- **Alternatives considered**: Raising a notification on every state change and filtering in the dashboard UI only — rejected, the OS notification center itself would still interrupt on every event, which is precisely what the user asked to avoid.
