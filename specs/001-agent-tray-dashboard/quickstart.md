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

### 5. Idle resource footprint (SC-006)

1. With no agents running (or agents idle) and the dashboard's own window
   closed, let the app sit in the tray/menu bar for several minutes.
2. Sample CPU and memory usage of the dashboard process (Task Manager /
   Activity Monitor) at rest, and note whether any other running
   application shows perceptible slowdown attributable to the dashboard.
3. **Expected**: The dashboard's idle CPU usage stays negligible (no
   sustained non-zero CPU between poll/status-check intervals) and no
   other application's responsiveness is perceptibly affected (SC-006).
   Record the observed idle CPU/memory figures for the record — SC-006 has
   no fixed numeric threshold (see spec.md), so this is a qualitative
   pass/fail judgment call, not an automated gate.

## Skipping hook setup

Repeat scenarios 1 and 2 without registering hooks.

**Expected**: Agents are still detected and listed, "Show" still works,
and each entry's activity shows as `Unknown` rather than a guessed value
(FR-013) — no notifications for idle/waiting-for-input are raised for
those sessions, since no activity signal exists to trigger them, but
session-ended notifications (sourced from process/window observation, not
hooks) still work.

## Automated coverage

These scenarios are backed by tasks.md's test suite:

- **Unit tests** against Application use cases with `IAgentWatcher`,
  `IAgentActivityFeed`, `IWindowFocuser`, and `INotifier` faked.
- **Integration tests** exercising the real Infrastructure implementations
  against actual local processes/windows/notifications/hook payloads per
  target OS.
- **Architecture tests** (`NetArchTest.Rules`) asserting the Domain layer
  has no outward dependencies and Infrastructure is only referenced from
  the composition root, per the constitution.

## Validation Log

### Windows — 2026-08-06

Machine: Windows 11 Pro, version 25H2 (build 10.0.26200) — the current
stable release channel, confirmed via `HKLM\SOFTWARE\Microsoft\Windows
NT\CurrentVersion\DisplayVersion` and `Win32_OperatingSystem.Caption`.
(An earlier note in this log incorrectly called this an Insider/Canary
build; it isn't — 26200 is 25H2 stable as of this machine's patch
level.)

- **T075 (Windows)**: Ran the full automated suite (68 passing, 9
  correctly skipped macOS-only tests) plus a series of manual app
  launches. The app builds, starts, stays resident, and shuts down
  cleanly every time; the hook listener genuinely binds and listens on
  `127.0.0.1:51820` (netstat-verified); `Win32WindowFocuser` was verified
  against a real window (minimize → Focus → restored, via
  `Win32WindowFocuserTests`); `WindowsToastNotifier` delivers real toasts
  for all three `AttentionReason`s (verified via `WindowsToastNotifierTests`,
  plus the `Setting.Enabled` check).
- **Finding — tray icon does not render visually on this machine**: the
  icon is never visible in the main tray or the overflow flyout, on any
  icon file tried. Diagnosed thoroughly, not just observed once:
  - Debug logging confirmed the full Avalonia-side path succeeds every
    time (`AssetLoader.Open` → `WindowIcon` construction → `TrayIcon.SetIcons`
    → `TrayIcon.GetIcons` count = 1).
  - Windows *does* register the icon — a `HKCU\Control Panel\NotifyIconSettings`
    entry appears with the correct exe path — but its cached `IconSnapshot`
    decodes to a fully transparent 32×32 image (verified by rendering it
    against a magenta background: solid magenta, zero content).
  - Ruled out the icon file as the cause: replaced it with
    `System.Drawing.SystemIcons.Application` (a known-good, real Windows
    icon) and got the identical blank-snapshot result.
  - Ruled out stale shell state: restarted `explorer.exe` between attempts;
    same result both before and after.
  - This isolates the problem to Avalonia's Win32 tray-icon rendering (or
    Shell_NotifyIcon's icon handling) on this machine, not to application
    code or the icon asset; everything downstream of "the icon is
    clickable" (list, Show, Dismiss, detail view, notifications) is
    validated independently via the automated suite and does not depend
    on this rendering path.
  - **Follow-up investigation (2026-08-06, same session)**: dug further
    after discovering the OS is stable 25H2, not Insider/Canary as
    previously (incorrectly) logged above, so an OS-build explanation no
    longer fit.
    - Found and fixed a real, separate bug while investigating: the
      Presentation `.csproj` had no `<ApplicationIcon>`, so the compiled
      `.exe` carried no embedded Win32 icon resource at all. Fixed by
      adding `<ApplicationIcon>Assets\tray-icon.ico</ApplicationIcon>`.
      Confirmed via `Icon.ExtractAssociatedIcon` against the rebuilt exe
      that the icon now embeds and decodes correctly — full blue circle
      with white dot, not blank. This also confirms `tray-icon.ico`
      itself is well-formed (byte-level ICONDIR/PNG-IHDR parsing checked
      out too: single 32×32 PNG-in-ICO entry, `colorType=6` RGBA,
      `bitDepth=8`, no truncation).
    - This incidentally explains an earlier, inconclusive side-experiment:
      pinning the icon via Settings → Taskbar → "Other system tray icons"
      showed our app's row rendering as a flat solid-blue square (no
      white dot) — before the fix that page was reading the exe's
      (missing) own icon resource, a completely different code path from
      the live `Shell_NotifyIcon` bitmap Avalonia sets at runtime. It
      does not exercise the runtime rendering path at all, so it can't
      confirm or rule out the overflow-vs-main-tray hypothesis.
    - **After the `ApplicationIcon` fix, re-ran the app and re-checked the
      live `HKCU\Control Panel\NotifyIconSettings` cached `IconSnapshot`
      for the running instance: still fully transparent** (rendered
      against a magenta background: solid magenta, zero content),
      identical to before the fix. This rules the exe's embedded icon
      resource in or out cleanly: it's out — the missing
      `ApplicationIcon` was a real bug worth fixing, but it was not the
      cause of the blank tray bitmap. The blank bitmap is specific to the
      HICON Avalonia constructs at runtime (via `CreateIconFromResourceEx`
      in `Win32Icon.cs`) and hands to `Shell_NotifyIcon`, not to the icon
      asset, the exe's resources, or the OS build/channel.
    - **Net conclusion**: root cause narrowed to Avalonia 12.1.1's
      Win32 tray-icon HICON construction/hand-off on this specific
      Windows 11 25H2 install, reproducible and isolated at every layer
      we could reach without attaching a native debugger to inspect the
      HICON's pixel data in-process (which would be the natural next
      step, along with testing a classic BMP+AND-mask ICO instead of
      PNG-compressed, and testing against a different Avalonia version).
      Not yet fixed; documented here rather than worked around, since a
      workaround (e.g., swapping to a non-Avalonia native tray icon P/Invoke
      path) would be a substantial scope change outside this diagnostic
      session.
- **T075 (SC-006 idle check)**: idle memory ~110–120MB, no sustained CPU
  observed at rest across multiple runs; no perceptible slowdown to other
  applications. Not measured with a profiler — Task Manager-level
  observation only.

### macOS — not executed

**T076 was not run.** No macOS hardware was available in this session.
The macOS-specific code (`MacProcessAgentWatcher`, `MacWindowFocuser`,
`MacUserNotifier`, `MacLoginItemRegistrar`) compiles and its tests are
correctly skip-guarded, but none of it has been executed on a real Mac.
In particular, `MacUserNotifier.NotificationActivated` is a documented,
known gap (see its class-level doc comment) independent of this.
