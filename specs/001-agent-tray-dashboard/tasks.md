---

description: "Task list for Agent Tray Dashboard implementation"

---

# Tasks: Agent Tray Dashboard

**Input**: Design documents from `/specs/001-agent-tray-dashboard/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/domain-ports.md,
contracts/hook-event-contract.md, quickstart.md

**Tests**: Included and sequenced test-first (write → confirm failing → implement) for the Domain,
Application, and Infrastructure layers. The project constitution (`.specify/memory/constitution.md`
v1.1.0, Principles II–III) makes this non-negotiable for those three layers, overriding the "tests are
optional" default. Presentation-layer tasks (Avalonia views, tray icon wiring, composition root) are
deliberately exempted from a preceding-test requirement per the same amendment — they are validated by the
mandatory manual quickstart.md scenarios instead (see Phase 7 T075/T076). This is a scope decision, not an
oversight; don't add unit tests for these tasks expecting them to satisfy Principle II.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and
testing of each story: Setup → Foundational → User Story 1 → User Story 2 → User Story 3 → User Story 4 →
Polish.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Every task includes an exact file path

## Path Conventions

Four-project Clean Architecture layout under `src/`, mirrored by four test projects under `tests/`, per
plan.md's Project Structure:

```text
src/ClaudeAgentDashboard.Domain/{Ports/}
src/ClaudeAgentDashboard.Application/UseCases/
src/ClaudeAgentDashboard.Infrastructure/{Windows,MacOS,Hooks,Settings}/
src/ClaudeAgentDashboard.Presentation/{TrayIcon,Views}/
tests/ClaudeAgentDashboard.Domain.UnitTests/
tests/ClaudeAgentDashboard.Application.UnitTests/
tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/
tests/ClaudeAgentDashboard.Architecture.Tests/
```

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and CI/analysis wiring, ahead of any feature code.

- [X] T001 Create the four `src/` projects and four `tests/` projects listed in plan.md's Project
      Structure, and update `ClaudeAgentDashboard.sln` to reference all eight, removing the placeholder
      `ClaudeAgentDashboard\ClaudeAgentDashboard.csproj` stub reference left over from initial scaffolding
- [X] T002 [P] Initialize `src/ClaudeAgentDashboard.Domain/ClaudeAgentDashboard.Domain.csproj` as a net8.0
      class library with zero external package references
- [X] T003 [P] Initialize `src/ClaudeAgentDashboard.Application/ClaudeAgentDashboard.Application.csproj` as
      a net8.0 class library referencing `ClaudeAgentDashboard.Domain`
- [X] T004 [P] Initialize `src/ClaudeAgentDashboard.Infrastructure/ClaudeAgentDashboard.Infrastructure.csproj`
      as a net8.0 class library referencing `ClaudeAgentDashboard.Domain` and `ClaudeAgentDashboard.Application`
      (the hook listener uses the built-in `System.Net.HttpListener` — no extra package needed, per
      research.md R9)
- [X] T005 [P] Initialize `src/ClaudeAgentDashboard.Presentation/ClaudeAgentDashboard.Presentation.csproj` as
      an Avalonia net8.0 desktop application referencing all three layers, with `Avalonia` and
      `Avalonia.Desktop` package references
- [X] T006 [P] Initialize `tests/ClaudeAgentDashboard.Domain.UnitTests`,
      `tests/ClaudeAgentDashboard.Application.UnitTests`,
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests`, and
      `tests/ClaudeAgentDashboard.Architecture.Tests` as xUnit net8.0 projects, each referencing its
      corresponding `src/` project (`Architecture.Tests` references all four `src/` projects)
- [X] T007 Add the `NetArchTest.Rules` package to `tests/ClaudeAgentDashboard.Architecture.Tests` and the
      `coverlet.collector` package to all four test projects
- [X] T008 [P] Add `Directory.Build.props` at the repo root wiring `SonarAnalyzer.CSharp` as a build-time
      analyzer with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` for new code
- [X] T009 [P] Add `sonar-project.properties` at the repo root configuring the SonarCloud project key and
      `src`/`tests` paths
- [X] T010 [P] Add `.github/workflows/ci.yml` building `ClaudeAgentDashboard.sln`, running all four test
      projects with `coverlet` coverage, and executing the SonarCloud scan

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, enums, port interfaces, the layering rule, and the minimal app shell every
user story builds on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T011 [P] Write architecture tests asserting Domain has zero outward dependencies, Application depends
      only on Domain, and Infrastructure implementations (including the `Hooks` subfolder) are referenced
      only from Presentation, in `tests/ClaudeAgentDashboard.Architecture.Tests/LayeringTests.cs` (these act
      as regression guards from this point forward — they pass trivially now and must keep passing as later
      phases add code)
- [X] T012 [P] Write failing unit tests for `AgentSession`: construction; the `SessionState`
      `Running → Ended` transition and `EndedAt` invariant; `ApplySignal(ActivitySignal)` mapping each
      `HookEvent` to the correct `ActivityState` (`UserPromptSubmit`/`PreToolUse` → `Working`, `Stop` →
      `Idle`, `Notification` → `WaitingForInput`, `SessionEnd` → forces `SessionState = Ended`); and the
      newest-timestamp-wins guard against out-of-order signals (spec edge case), in
      `tests/ClaudeAgentDashboard.Domain.UnitTests/AgentSessionTests.cs`
- [X] T013 [P] Write failing unit tests for `TerminalWindowReference`'s one-way `IsResolvable` transition in
      `tests/ClaudeAgentDashboard.Domain.UnitTests/TerminalWindowReferenceTests.cs`
- [X] T014 [P] Write failing unit tests for `AttentionNotification` construction and its
      `AgentSessionId`/`Reason` linkage in
      `tests/ClaudeAgentDashboard.Domain.UnitTests/AttentionNotificationTests.cs`
- [X] T015 [P] Implement the `SessionState` enum (`Running`, `Ended`) in
      `src/ClaudeAgentDashboard.Domain/SessionState.cs`
- [X] T016 [P] Implement the `ActivityState` enum (`Unknown`, `Working`, `Idle`, `WaitingForInput`) in
      `src/ClaudeAgentDashboard.Domain/ActivityState.cs`
- [X] T017 [P] Implement the `HookEvent` enum (`UserPromptSubmit`, `PreToolUse`, `Stop`, `Notification`,
      `SessionEnd`) in `src/ClaudeAgentDashboard.Domain/HookEvent.cs`
- [X] T018 [P] Implement the `AttentionReason` enum (`Idle`, `WaitingForInput`, `Ended`) in
      `src/ClaudeAgentDashboard.Domain/AttentionReason.cs`
- [X] T019 [P] Implement the `ActivitySignal` type (correlation key, `HookEvent`, `OccurredAt`,
      `SummaryText`) in `src/ClaudeAgentDashboard.Domain/ActivitySignal.cs` (depends on T017)
- [X] T020 Implement the `AgentSession` entity, including `ApplySignal`, in
      `src/ClaudeAgentDashboard.Domain/AgentSession.cs`, making T012 pass (depends on T015, T016, T019)
- [X] T021 [P] Implement the `TerminalWindowReference` entity in
      `src/ClaudeAgentDashboard.Domain/TerminalWindowReference.cs`, making T013 pass
- [X] T022 [P] Implement the `AttentionNotification` entity in
      `src/ClaudeAgentDashboard.Domain/AttentionNotification.cs`, making T014 pass (depends on T018)
- [X] T023 [P] Define the `IAgentWatcher` port (`GetCurrentSessions`, `SessionStarted`, `SessionEnded`) in
      `src/ClaudeAgentDashboard.Domain/Ports/IAgentWatcher.cs`
- [X] T024 [P] Define the `IAgentActivityFeed` port (`SignalReceived`) in
      `src/ClaudeAgentDashboard.Domain/Ports/IAgentActivityFeed.cs`
- [X] T025 [P] Define the `IWindowFocuser` port and `FocusResult` type in
      `src/ClaudeAgentDashboard.Domain/Ports/IWindowFocuser.cs`
- [X] T026 [P] Define the `INotifier` port (`NotifyAttention`, `NotificationActivated`) in
      `src/ClaudeAgentDashboard.Domain/Ports/INotifier.cs`
- [X] T027 [P] Define the `IHookRegistrar` port (`AreHooksRegistered`, `RegisterHooks`) in
      `src/ClaudeAgentDashboard.Domain/Ports/IHookRegistrar.cs`
- [X] T028 [P] Define the `ISettingsStore` port in `src/ClaudeAgentDashboard.Domain/Ports/ISettingsStore.cs`
- [X] T029 Implement the Avalonia application shell and composition root —
      `src/ClaudeAgentDashboard.Presentation/Program.cs`, `App.axaml`/`App.axaml.cs`, and
      `src/ClaudeAgentDashboard.Presentation/CompositionRoot.cs` with OS-conditional DI registration stubs
      for all six ports (depends on T023–T028) — Presentation task, no preceding test per constitution
      v1.1.0 Principle II/III
- [X] T030 Implement `TrayIconController` showing a persistent tray/menu-bar icon with a Quit action, in
      `src/ClaudeAgentDashboard.Presentation/TrayIcon/TrayIconController.cs` (depends on T029) — satisfies
      the baseline "always-present icon" requirement (FR-001) all stories build on; Presentation task, no
      preceding test per constitution v1.1.0

**Checkpoint**: Foundation ready — user story implementation can now begin.

---

## Phase 3: User Story 1 - See all running agents at a glance (Priority: P1) 🎯 MVP

**Goal**: Clicking the tray/menu-bar icon opens a window listing every currently running Claude Code
agent and its status, updating as agents start, with an empty state when none are running.

**Independent Test**: Start several Claude Code CLI sessions in different terminal windows, click the
tray/menu-bar icon, and confirm the popover lists one entry per running agent, updating as new agents
start — see quickstart.md scenario 1.

### Tests for User Story 1

> Write these tests FIRST, confirm they FAIL, then implement.

- [X] T031 [P] [US1] Write failing integration test: spawn a real process matching the Claude Code CLI
      signature **before** constructing/starting the watcher, then assert
      `WindowsProcessAgentWatcher.GetCurrentSessions()` finds it — proving the "already running before the
      app started" path (FR-010), not just "starts while watching" — in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/WindowsProcessAgentWatcherTests.cs`
      (Windows-only)
- [X] T032 [P] [US1] Write failing integration test: spawn a real process matching the Claude Code CLI
      signature **before** constructing/starting the watcher, then assert
      `MacProcessAgentWatcher.GetCurrentSessions()` finds it — proving the "already running before the app
      started" path (FR-010), not just "starts while watching" — in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/MacProcessAgentWatcherTests.cs`
      (macOS-only)
- [X] T033 [P] [US1] Write failing unit test: `OpenDashboardQuery` returns every session (already-running
      and newly started) from a faked `IAgentWatcher`, and an empty result when none are running, in
      `tests/ClaudeAgentDashboard.Application.UnitTests/OpenDashboardQueryTests.cs`

### Implementation for User Story 1

- [X] T034 [US1] Implement `WindowsProcessAgentWatcher` (WMI process enumeration + command-line matching +
      `SessionStarted` on newly detected processes; `GetCurrentSessions()` also finds processes that
      started before the watcher did) in
      `src/ClaudeAgentDashboard.Infrastructure/Windows/WindowsProcessAgentWatcher.cs`, making T031 pass
      (depends on T020, T023)
- [X] T035 [US1] Implement `MacProcessAgentWatcher` (`ps` enumeration + command-line matching +
      `SessionStarted`; `GetCurrentSessions()` also finds processes that started before the watcher did) in
      `src/ClaudeAgentDashboard.Infrastructure/MacOS/MacProcessAgentWatcher.cs`, making T032 pass (depends
      on T020, T023)
- [X] T036 [US1] Implement `OpenDashboardQuery` in
      `src/ClaudeAgentDashboard.Application/UseCases/OpenDashboardQuery.cs`, making T033 pass
- [X] T037 [US1] Implement `AgentListWindow` (list bound to sessions showing `SessionState` and
      `ActivityState` — the latter naturally `Unknown` until User Story 3's activity feed exists — plus
      empty state) in `src/ClaudeAgentDashboard.Presentation/Views/AgentListWindow.axaml` and `.axaml.cs`
      (depends on T036) — Presentation task, no preceding test per constitution v1.1.0
- [X] T038 [US1] Wire tray icon click → `OpenDashboardQuery` → `AgentListWindow`, and register the
      OS-appropriate `IAgentWatcher` in `CompositionRoot`, in
      `src/ClaudeAgentDashboard.Presentation/TrayIcon/TrayIconController.cs` and
      `src/ClaudeAgentDashboard.Presentation/CompositionRoot.cs` (depends on T034, T035, T037) —
      Presentation task, no preceding test per constitution v1.1.0

**Checkpoint**: User Story 1 is fully functional and independently testable.

---

## Phase 4: User Story 2 - Jump straight to an agent's window (Priority: P1)

**Goal**: Clicking "Show" on a listed agent brings that agent's terminal window to the foreground and
focuses it, informing the user instead of failing silently if that window is gone.

**Independent Test**: With one or more agents running, click "Show" on a specific list entry and confirm
the correct terminal window is raised and focused, including when minimized — see quickstart.md scenario 2.

### Tests for User Story 2

> Write these tests FIRST, confirm they FAIL, then implement.

- [X] T039 [P] [US2] Write failing integration test: `Win32WindowFocuser` brings a real spawned window to
      the foreground and reports success; reports "not available" once that window is closed, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/Win32WindowFocuserTests.cs` (Windows-only)
- [X] T040 [P] [US2] Write failing integration test: `MacWindowFocuser` activates a real running
      application and reports success; reports "not available" once it has quit, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/MacWindowFocuserTests.cs` (macOS-only)
- [X] T041 [P] [US2] Write failing unit test: `ShowAgentCommand` calls `IWindowFocuser.Focus` with the
      session's `TerminalWindowReference` and surfaces the `FocusResult`, using a faked `IWindowFocuser`,
      in `tests/ClaudeAgentDashboard.Application.UnitTests/ShowAgentCommandTests.cs`

### Implementation for User Story 2

- [X] T042 [US2] Implement `Win32WindowFocuser` (`EnumWindows`/`GetWindowThreadProcessId`/
      `SetForegroundWindow`, with the `AttachThreadInput` foreground-lock workaround) in
      `src/ClaudeAgentDashboard.Infrastructure/Windows/Win32WindowFocuser.cs`, making T039 pass (depends on
      T021, T025)
- [X] T043 [US2] Implement `MacWindowFocuser` (`NSRunningApplication.activateWithOptions` interop) in
      `src/ClaudeAgentDashboard.Infrastructure/MacOS/MacWindowFocuser.cs`, making T040 pass (depends on
      T021, T025)
- [X] T044 [US2] Implement `ShowAgentCommand` in
      `src/ClaudeAgentDashboard.Application/UseCases/ShowAgentCommand.cs`, making T041 pass
- [X] T045 [US2] Add a "Show" button and FR-011 "window no longer available" messaging to `AgentListWindow`
      in `src/ClaudeAgentDashboard.Presentation/Views/AgentListWindow.axaml` and `.axaml.cs` (depends on
      T044; touches the same file as T037 — sequence after it) — Presentation task, no preceding test per
      constitution v1.1.0
- [X] T046 [US2] Register the OS-appropriate `IWindowFocuser` in `CompositionRoot`, in
      `src/ClaudeAgentDashboard.Presentation/CompositionRoot.cs` (depends on T042, T043) — Presentation
      task, no preceding test per constitution v1.1.0

**Checkpoint**: User Stories 1 AND 2 both work independently.

---

## Phase 5: User Story 3 - Get notified only when an agent needs me (Priority: P2)

**Goal**: An OS-native notification appears the moment an agent stops actively working — goes idle, needs
input, or its session ends — and never while it is merely working; clicking it focuses the correct window
without opening the dashboard first, the list reflects status live, and ended agents can be dismissed.

**Independent Test**: With hooks registered, watch an agent work (no notification), then let it go idle,
ask a permission question, or end its session (each raises a distinct notification); click a notification
and confirm it focuses the correct window; dismiss an ended entry and confirm it leaves the list — see
quickstart.md scenario 3.

### Tests for User Story 3

> Write these tests FIRST, confirm they FAIL, then implement.

- [X] T047 [P] [US3] Write failing integration test: `WindowsProcessAgentWatcher` raises `SessionEnded`
      within the poll interval after a tracked process exits, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/WindowsProcessAgentWatcherTests.cs`
- [X] T048 [P] [US3] Write failing integration test: `MacProcessAgentWatcher` raises `SessionEnded` within
      the poll interval after a tracked process exits, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/MacProcessAgentWatcherTests.cs`
- [X] T049 [P] [US3] Write failing integration test: `HookEventListener` parses a valid payload on each of
      the five routes in contracts/hook-event-contract.md into the correct `ActivitySignal`
      (`HookEvent` + correlation key + `SummaryText`), and responds with a `4xx` without crashing for a
      malformed payload, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/HookEventListenerTests.cs`
- [X] T050 [P] [US3] Write failing integration test: `ClaudeCodeHookRegistrar` writes the five expected hook
      commands into a temporary Claude Code config file pointed at a given listener address, and is
      idempotent (a second call updates in place rather than duplicating entries), in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/ClaudeCodeHookRegistrarTests.cs`
- [X] T051 [P] [US3] Write failing unit test: `ApplyActivitySignalCommand` correlates an incoming
      `ActivitySignal` to the right `AgentSession` by `cwd`/`session_id` (R10), applies the
      newest-timestamp-wins rule, and calls `INotifier.NotifyAttention` only on a transition into `Idle`,
      `WaitingForInput`, or `Ended` from a genuinely different state — never for `Working`, never twice for
      the same unacknowledged attention state (FR-007, FR-007a, R11) — using a faked session store and
      faked `INotifier`, in
      `tests/ClaudeAgentDashboard.Application.UnitTests/ApplyActivitySignalCommandTests.cs`
- [X] T052 [P] [US3] Write failing integration test: `WindowsToastNotifier` delivers a real toast for each
      `AttentionReason` and raises `NotificationActivated` when clicked, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/WindowsToastNotifierTests.cs`
      (Windows-only)
- [X] T053 [P] [US3] Write failing integration test: `MacUserNotifier` delivers a real `UNUserNotification`
      for each `AttentionReason`, raises `NotificationActivated` when clicked, and reports
      `WasDelivered = false` when authorization is denied, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/MacUserNotifierTests.cs` (macOS-only)
- [X] T054 [P] [US3] Write failing unit test: `HandleNotificationActivatedCommand` resolves the correct
      session's `TerminalWindowReference` and calls `IWindowFocuser.Focus` when
      `INotifier.NotificationActivated` fires, using faked `INotifier` and `IWindowFocuser`, in
      `tests/ClaudeAgentDashboard.Application.UnitTests/HandleNotificationActivatedCommandTests.cs`
- [X] T055 [P] [US3] Write failing unit test: `DismissAgentCommand` removes an `Ended` session from the
      active list and is a no-op for a `Running` session, using a faked session store, in
      `tests/ClaudeAgentDashboard.Application.UnitTests/DismissAgentCommandTests.cs` (closes FR-012's
      missing coverage — surfaced by `/speckit-analyze` finding K1)

### Implementation for User Story 3

- [X] T056 [US3] Extend `WindowsProcessAgentWatcher` with `SessionEnded` detection, making T047 pass
      (depends on T034)
- [X] T057 [US3] Extend `MacProcessAgentWatcher` with `SessionEnded` detection, making T048 pass (depends
      on T035)
- [X] T058 [US3] Implement `HookEventListener` (loopback `HttpListener` hosting the five routes from
      contracts/hook-event-contract.md, implementing `IAgentActivityFeed`), making T049 pass, in
      `src/ClaudeAgentDashboard.Infrastructure/Hooks/HookEventListener.cs` (depends on T019, T024)
- [X] T059 [US3] Implement `ClaudeCodeHookRegistrar` (`IHookRegistrar`), making T050 pass, in
      `src/ClaudeAgentDashboard.Infrastructure/Hooks/ClaudeCodeHookRegistrar.cs` (depends on T027)
- [X] T060 [US3] Implement `ApplyActivitySignalCommand` (correlation per R10, `AgentSession.ApplySignal`,
      and the notify-decision per R11), making T051 pass, in
      `src/ClaudeAgentDashboard.Application/UseCases/ApplyActivitySignalCommand.cs`
- [X] T061 [US3] Implement `WindowsToastNotifier`, making T052 pass, in
      `src/ClaudeAgentDashboard.Infrastructure/Windows/WindowsToastNotifier.cs` (depends on T022, T026)
- [X] T062 [US3] Implement `MacUserNotifier`, making T053 pass, in
      `src/ClaudeAgentDashboard.Infrastructure/MacOS/MacUserNotifier.cs` (depends on T022, T026)
- [X] T063 [US3] Implement `HandleNotificationActivatedCommand`, making T054 pass, in
      `src/ClaudeAgentDashboard.Application/UseCases/HandleNotificationActivatedCommand.cs`
- [X] T064 [US3] Implement `DismissAgentCommand` in
      `src/ClaudeAgentDashboard.Application/UseCases/DismissAgentCommand.cs`, making T055 pass (K1: this
      was listed in plan.md's Project Structure but had no task until this fix)
- [X] T065 [US3] Update `AgentListWindow` to reflect live `SessionState`/`ActivityState` changes and wire a
      Dismiss action for ended entries to `DismissAgentCommand` (not direct state manipulation — keeps
      Presentation calling only into Application, per constitution Principle I), and update
      `TrayIconController` to badge agents in an unacknowledged attention-needed state (FR-009), in
      `src/ClaudeAgentDashboard.Presentation/Views/AgentListWindow.axaml`/`.axaml.cs` and
      `src/ClaudeAgentDashboard.Presentation/TrayIcon/TrayIconController.cs` (depends on T060, T063, T064;
      touches the same `AgentListWindow` file as T037/T045 — sequence after them) — Presentation task, no
      preceding test per constitution v1.1.0
- [X] T066 [US3] Add a "Set up activity detection" tray menu action calling `IHookRegistrar.RegisterHooks`
      (the FR-013 one-time setup step) in
      `src/ClaudeAgentDashboard.Presentation/TrayIcon/TrayIconController.cs` (depends on T059) —
      Presentation task, no preceding test per constitution v1.1.0
- [X] T067 [US3] Register `IAgentActivityFeed`, `IHookRegistrar`, and the OS-appropriate `INotifier` in
      `CompositionRoot`, and start `HookEventListener` at app startup, in
      `src/ClaudeAgentDashboard.Presentation/CompositionRoot.cs` (depends on T058, T059, T060, T061, T062,
      T063) — Presentation task, no preceding test per constitution v1.1.0

**Checkpoint**: User Stories 1, 2, and 3 are all independently functional.

---

## Phase 6: User Story 4 - See what an agent is currently doing (Priority: P3)

**Goal**: Clicking an agent's list entry (not "Show") opens a detail view, inside the dashboard, with a
human-readable summary of its current activity, updating live as that activity changes.

**Independent Test**: With hooks registered and an agent running, click its list entry and confirm a
detail view opens showing its current activity, updating as that activity changes — see quickstart.md
scenario 4. (Depends on User Story 3's hook infrastructure existing to have any activity content to show,
per plan.md; the view itself is a separate, independently-addable slice.)

### Tests for User Story 4

> Write this test FIRST, confirm it FAILS, then implement.

- [ ] T068 [P] [US4] Write failing unit test: `ViewAgentActivityQuery` returns the current `ActivityState`
      and `ActivitySummary` for a session, reflecting the most recently applied `ActivitySignal`, using a
      faked session store, in
      `tests/ClaudeAgentDashboard.Application.UnitTests/ViewAgentActivityQueryTests.cs`

### Implementation for User Story 4

- [ ] T069 [US4] Implement `ViewAgentActivityQuery` in
      `src/ClaudeAgentDashboard.Application/UseCases/ViewAgentActivityQuery.cs`, making T068 pass
- [ ] T070 [US4] Implement `AgentActivityDetailView` (live-updating activity summary) in
      `src/ClaudeAgentDashboard.Presentation/Views/AgentActivityDetailView.axaml` and `.axaml.cs` (depends
      on T069) — Presentation task, no preceding test per constitution v1.1.0
- [ ] T071 [US4] Wire a click on an agent's list entry (distinct from "Show") in `AgentListWindow` to open
      `AgentActivityDetailView` via `ViewAgentActivityQuery`, in
      `src/ClaudeAgentDashboard.Presentation/Views/AgentListWindow.axaml`/`.axaml.cs` (depends on T070;
      touches the same file as T037/T045/T065 — sequence after them) — Presentation task, no preceding
      test per constitution v1.1.0

**Checkpoint**: All four user stories are independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that support the feature as a whole without belonging to a single user story.

- [ ] T072 [P] Write failing integration test for `JsonSettingsStore` round-tripping
      `LaunchAtLoginEnabled` against a real temp file, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/JsonSettingsStoreTests.cs`
- [ ] T073 Implement `JsonSettingsStore` in
      `src/ClaudeAgentDashboard.Infrastructure/Settings/JsonSettingsStore.cs`, making T072 pass (depends
      on T028)
- [ ] T074 [P] Register the app as an OS login item (Windows Run registry key / macOS LaunchAgent), gated
      by `ISettingsStore.LaunchAtLoginEnabled`, wired in
      `src/ClaudeAgentDashboard.Presentation/CompositionRoot.cs` (depends on T073)
- [ ] T075 [P] Run quickstart.md validation end-to-end on Windows — all four user-story scenarios, the
      skip-hook-setup scenario, **and the idle resource-footprint check (SC-006)** — and record results in
      `specs/001-agent-tray-dashboard/quickstart.md` (C1: SC-006 previously had no validation step at all)
- [ ] T076 [P] Run quickstart.md validation end-to-end on macOS — all four user-story scenarios, the
      skip-hook-setup scenario, **and the idle resource-footprint check (SC-006)** — and record results in
      `specs/001-agent-tray-dashboard/quickstart.md` (C1)
- [ ] T077 [P] Review the SonarCloud/coverage report from CI and address any new-code issues surfaced
- [ ] T078 [P] Update `README.md` with build, run, hook-setup, and architecture overview instructions
      referencing plan.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001–T010)**: No dependencies — start immediately; most tasks parallel.
- **Foundational (T011–T030)**: Depends on Setup completion — BLOCKS all user stories.
- **User Story 1 (T031–T038)**: Depends on Foundational only.
- **User Story 2 (T039–T046)**: Depends on Foundational only; independently testable from US1, but T045
  edits the same `AgentListWindow` file T037 creates, so sequence after it if worked serially.
- **User Story 3 (T047–T067)**: Depends on Foundational; extends the watchers US1 implements (T034, T035)
  and the `AgentListWindow`/`TrayIconController` files earlier stories touch — independently testable per
  its own acceptance scenarios, but naturally sequenced after US1/US2.
- **User Story 4 (T068–T071)**: Depends on Foundational directly, and in practice on User Story 3's hook
  pipeline (T058–T067) to have any `ActivitySummary` content to display — sequenced last among the stories.
- **Polish (T072–T078)**: Depends on the desired user stories being complete.

### Within Each User Story

- Tests are written and confirmed failing before implementation for Domain/Application/Infrastructure
  tasks (constitution Principle II, v1.1.0); Presentation tasks are explicitly exempted (see the Tests
  note at the top of this file) and validated via T075/T076 instead.
- Domain/Application layers before Infrastructure before Presentation wiring.
- Story checkpoint reached before moving to the next priority.

### Parallel Opportunities

- Setup: T002–T006, T008–T010 in parallel.
- Foundational: T011–T014 in parallel; T015–T019 in parallel; T021, T022 in parallel; T023–T028 in
  parallel; T020 depends on T015/T016/T019.
- Per user story, the Windows and macOS integration tests/implementations are different files and can run
  in parallel (e.g., T031 ∥ T032; T039 ∥ T040; T047 ∥ T048; T052 ∥ T053).
- Within User Story 3, T047–T055 (nine independent test files) can all be written in parallel before any
  implementation begins.
- Different user stories can be staffed in parallel once Foundational is complete, keeping in mind the
  shared-file sequencing notes on `AgentListWindow`/`TrayIconController` above.

---

## Parallel Example: User Story 3

```bash
# Launch all US3 test-writing together:
Task: "Integration test WindowsProcessAgentWatcher SessionEnded in tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/WindowsProcessAgentWatcherTests.cs"
Task: "Integration test MacProcessAgentWatcher SessionEnded in tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/MacProcessAgentWatcherTests.cs"
Task: "Integration test HookEventListener payload parsing in tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/HookEventListenerTests.cs"
Task: "Integration test ClaudeCodeHookRegistrar idempotent write in tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/ClaudeCodeHookRegistrarTests.cs"
Task: "Unit test ApplyActivitySignalCommand correlation + notify-decision in tests/ClaudeAgentDashboard.Application.UnitTests/ApplyActivitySignalCommandTests.cs"
Task: "Unit test DismissAgentCommand in tests/ClaudeAgentDashboard.Application.UnitTests/DismissAgentCommandTests.cs"

# Then the notifier implementations together:
Task: "Implement WindowsToastNotifier in src/ClaudeAgentDashboard.Infrastructure/Windows/WindowsToastNotifier.cs"
Task: "Implement MacUserNotifier in src/ClaudeAgentDashboard.Infrastructure/MacOS/MacUserNotifier.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: run quickstart.md scenario 1 independently.
5. Demo: a working "see all detected agents" tray app.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. User Story 1 → validate → demo (MVP).
3. User Story 2 → validate → demo (can now jump to any agent's window).
4. User Story 3 → validate → demo (the full "notify me only when it needs me" loop, plus dismissing ended
   agents — the heart of this revision).
5. User Story 4 → validate → demo (per-agent activity detail view).
6. Polish → login-item registration, cross-platform validation sign-off (including SC-006), README.

---

## Notes

- [P] tasks touch different files with no unmet dependencies.
- [Story] labels trace each task back to its spec.md user story.
- Tests are mandatory for Domain/Application/Infrastructure per the project constitution (v1.1.0), not
  optional — confirm each test fails before writing the implementation that makes it pass. Presentation
  tasks are annotated inline as intentionally exempt (see the Tests note at the top of this file).
- Commit after each task or logical group.
- Stop at any checkpoint to validate a story independently before continuing.
