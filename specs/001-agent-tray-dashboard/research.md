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
