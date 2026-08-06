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
3. **Expected**: The dashboard window opens within 2 seconds (SC-001)
   showing one card per session on the desktop surface, each marked
   `Running`.
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
   that agent's card (not "Show").
2. **Expected**: A detail overlay opens within 2 seconds (SC-007), inside
   the same dashboard window, describing the current activity (e.g., the
   tool being run) and offering that agent's actions (Show, and Dismiss
   once ended).
3. Leave the overlay open and let the agent's activity change (e.g., it
   finishes the tool and starts waiting for input).
4. **Expected**: The overlay updates in place to reflect the new activity
   without being reopened.
5. Close the overlay.
6. **Expected**: The dashboard returns to the card view in the same
   window — no separate window closes or is left behind.

### 5. Arrange and personalize the desktop (User Story 5)

1. With two or more agents running, open the dashboard and drag one
   agent's card to a new position.
2. **Expected**: The card stays exactly where dropped (SC-008); no
   auto-reflow or snap-back.
3. Restart the application.
4. **Expected**: That agent's card reappears at the position it was
   dragged to.
5. Open the background customization action and select an image file.
6. **Expected**: The dashboard surface immediately shows that image as
   its background.
7. Restart the application.
8. **Expected**: The same background is shown without reselecting it
   (SC-009).

### 6. Activity detection correlates correctly for an ordinary session (SC-010, FR-018)

Prerequisite: hooks registered (see setup step above).

1. Start a Claude Code CLI session with the plain `claude` command — no
   extra arguments — the ordinary case.
2. Give it a task so it runs a tool.
3. **Expected**: Within the existing live-refresh interval, the card and
   detail overlay show `Working`, not `Unknown` — no restart, no manual
   action needed beyond the one-time hook setup.
4. Open that agent's detail overlay.
5. **Expected**: A read-only section shows recent content from the
   session's transcript, refreshing as the conversation continues
   (FR-019). No control that sends input back to the agent is present
   anywhere in this view.

### 7. Idle resource footprint (SC-006)

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
- **T089 (User Story 5 — desktop/card UI) — 2026-08-06**: `DesktopWindow`
  opened showing the real, currently-running Claude Code CLI session
  (`"C:\Users\louis\.local\bin\claude.exe"`) as a card with icon/label/
  status ("Running", gray dot — correct, since hooks aren't registered on
  this run). Verified live, end-to-end, via UI Automation + screenshots
  (not just code review):
  - Clicking the card opened `AgentDetailOverlay` in place over the same
    window (with a dimmed scrim behind it), showing the full label, live
    "Unknown (activity detection requires hook setup)" status text, and a
    "Show" button (Dismiss correctly hidden — session isn't `Ended`).
  - Clicking the overlay's close button returned to the card view in the
    same window — no second window involved at any point (FR-014).
  - Dragging the card (simulated pointer down → move → up) moved it
    smoothly and, on release, wrote its new position to
    `%APPDATA%\ClaudeAgentDashboard\settings.json` under
    `CardPositions` keyed by the agent's label — confirmed by reading the
    file directly.
  - Restarted the application (`taskkill` + relaunch) and confirmed the
    card reappeared at exactly the dragged-to position — SC-008 verified
    across a real restart, not just via the `JsonSettingsStore` unit-level
    round-trip tests (T079/T080, also passing).
  - The "Choose background…" button is present and wired to
    `StorageProvider.OpenFilePickerAsync`; **not** exercised interactively
    in this pass (a real file-picker dialog isn't safely automatable via
    UI Automation without risking a stuck modal), so SC-009's actual file
    selection + persistence + restart round-trip was validated only at
    the `ISettingsStore`/`JsonSettingsStore` unit level (T080), not
    end-to-end through the picker UI. Worth a manual pass before calling
    this fully signed off.
  - Verification required bypassing the tray icon (still affected by the
    unresolved blank-bitmap issue above — Explorer's overflow flyout
    exposes it as one of several unnamed icons with no reliable way to
    tell them apart from outside the process) via a temporary
    `--open-dashboard` CLI switch added and removed for this session only;
    it is not present in the committed code.
- **Diagnosed — activity detection correlation bug (FR-018), found while
  dogfooding, 2026-08-06**: a real Claude Code CLI session (this app's own
  running session, started as a plain `claude` command) stayed `Unknown`
  even with hooks confirmed registered in `~/.claude/settings.json`
  pointing at the live listener (`127.0.0.1:51820`). Diagnosed empirically
  by posting synthetic payloads directly to the running listener with
  `curl`, not by inspection alone:
  - `POST /hooks/pre-tool-use` with a realistic `cwd`
    (`.../ClaudeAgentDashboard`) → `200 OK` (accepted and parsed), but the
    card stayed `Unknown` — the signal was silently dropped at
    correlation, not at ingestion.
  - The same route with a `cwd` deliberately crafted to substring-overlap
    the session's command-line-derived label → the card turned `Working`
    (blue dot, badge text) within the next 2-second poll, with **no**
    manual refresh, restart, or any other action taken. This isolates the
    defect precisely: the live-update/rendering pipeline is correct
    end-to-end; only the correlation match (R10) is broken for the
    ordinary case, because `WindowsProcessAgentWatcher` has no way to
    learn a process's actual working directory from WMI and falls back to
    the full command line, which for a bare `claude` invocation never
    contains it.
  - Fix tracked as R15/FR-018 (PEB-based working-directory resolution) —
    not yet implemented as of this log entry.
- **T109 (correlation fix + transcript display, FR-018/FR-019) — fixed and
  verified live, 2026-08-06**: implemented `WindowsWorkingDirectoryResolver`
  (PEB reading via `NtQueryInformationProcess` + `ReadProcessMemory`) and
  wired it into `WindowsProcessAgentWatcher`; `AgentSessionRegistry`
  updated to prefer the resolved `WorkingDirectory` over the label. All 18
  new automated tests (T091–T108) pass — full suite 100 passed, 9 skipped
  (macOS-only), 0 failed. Beyond the automated suite, re-ran the exact
  live diagnostic from the earlier bug report against this session's own
  real Claude Code CLI process, on the running app:
  - Re-sent the same synthetic `cwd` (this repo's real path) that was
    previously silently ignored — this time the card turned `Working`
    (blue badge) within the next poll, with no restart. Confirms
    `WindowsWorkingDirectoryResolver` correctly resolved this real
    process's actual working directory and correlation now uses it.
  - Sent a payload additionally carrying a `transcript_path` pointing at a
    real temp JSONL file with three lines (a `user` message, an
    `assistant` text message, and an `assistant` tool-use block with no
    `text` field). The overlay's new "Recent transcript" section rendered
    the first two as `role: text`, and the third — which the schema-
    tolerant reader couldn't extract clean text from — as its raw JSON
    line, confirming the designed graceful-fallback behavior (R16) rather
    than dropping or crashing on an entry it didn't fully understand.
  - The overlay's hook-setup guidance text was also corrected (T108) to
    stop suggesting "restart this session" once hooks are registered,
    since that was never the actual fix.
  - Not verified: real Claude Code hook payloads were not observed
    directly in this session (all transcript verification used a
    synthetic payload); the `transcript_path` field's presence/shape in
    genuine Claude Code hook traffic is assumed from the wire contract,
    not confirmed against a live Claude Code hook invocation.
- **Important follow-up finding, same day**: after all of the above, the
  live dashboard still showed this session's own agent as `Unknown` —
  including after several more real tool calls, which should have fired
  real `PreToolUse` hooks if anything was reaching the listener. Added
  temporary request logging to `HookEventListener` (removed afterward) and
  confirmed **zero requests arrived** from real tool-call activity in this
  session, despite the listener genuinely working (proven moments earlier
  via `curl`-based synthetic payloads) and `AreHooksRegistered()` reporting
  `true`. Root cause, confirmed via Claude Code's own public issue tracker
  (not guessed): **Claude Code snapshots its hook configuration once, at
  session start, and does not re-read it mid-session** — a deliberate
  security measure, not a bug on either side. This session's hooks were
  registered mid-conversation, long after the session itself started, so
  its snapshot never included them; no amount of waiting or activity will
  ever make it report status without a restart. This is a *different*
  failure mode from the correlation bug fixed above — both are real, and
  the app can't tell from the outside which one applies to a given
  `Unknown` session, so `AgentDetailOverlay`'s guidance text was corrected
  again to mention both explicitly (a session's own restart status can't
  be introspected, only ruled in/out by trying it).
  - **Not independently re-verified**: this specific dashboard session's
    own status was never confirmed to start correctly reporting after an
    actual restart, since restarting this conversation's own CLI session
    was not something achievable mid-conversation. The Claude Code issue
    tracker finding is treated as sufficient evidence for the mechanism,
    but the dashboard's specific behavior after a real restart — as
    opposed to a synthetic signal — should get one real pass before
    considering FR-013/FR-018 fully closed.

### macOS — not executed

**T076 was not run.** No macOS hardware was available in this session.
The macOS-specific code (`MacProcessAgentWatcher`, `MacWindowFocuser`,
`MacUserNotifier`, `MacLoginItemRegistrar`) compiles and its tests are
correctly skip-guarded, but none of it has been executed on a real Mac.
The same applies to T101's `lsof`-based working-directory resolution
added to `MacProcessAgentWatcher` — implemented to the same contract as
the Windows fix (best-effort, null on any failure) but not run against a
real Claude Code CLI session on macOS.
In particular, `MacUserNotifier.NotificationActivated` is a documented,
known gap (see its class-level doc comment) independent of this.
